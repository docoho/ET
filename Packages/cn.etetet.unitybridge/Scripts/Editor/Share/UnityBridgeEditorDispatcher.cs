using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using UnityEditor;

namespace ET
{
    // Dispatcher resolves bridge commands from the current loaded code domain.
    // Keep this file tiny so Unity refreshes and reloads it quickly during bridge iteration.
    /// <summary>
    /// 单个 UnityBridge 命令处理器的注册项元数据。
    /// </summary>
    internal sealed class UnityBridgeHandlerRegistration
    {
        public Type RequestType { get; set; }

        public Type ResponseType { get; set; }

        public IUnityBridgeHandler Handler { get; set; }
    }

    /// <summary>
    /// UnityBridge 编辑器端命令分发器，利用 TypeCache 自动发现所有 IUnityBridgeHandler 实现类，负责请求 JSON 规范化反序列化与路由派发。
    /// </summary>
    internal static class UnityBridgeEditorDispatcher
    {
        private static readonly Dictionary<Type, UnityBridgeHandlerRegistration> Registrations = BuildRegistrations();
        private static readonly Dictionary<string, UnityBridgeHandlerRegistration> RegistrationsByName = BuildRegistrationsByName(Registrations);
        private static bool mongoInitialized;

        public static string[] GetAvailableCommandTypes()
        {
            return Registrations.Keys
                    .Select(static value => value.Name)
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .OrderBy(static value => value, StringComparer.Ordinal)
                    .ToArray();
        }

        public static bool TryParse(UnityBridgeRequestEnvelope request, out IRequest command, out string commandTypeName, out string error)
        {
            command = null;
            commandTypeName = string.Empty;
            error = null;

            EnsureMongoRegistered();

            if (request == null)
            {
                error = "unity bridge request is null";
                return false;
            }

            if (!TryNormalizeCommandJson(
                    request.CommandJson,
                    RegistrationsByName,
                    out string rawCommandJson,
                    out commandTypeName,
                    out UnityBridgeHandlerRegistration registration,
                    out error))
            {
                return false;
            }

            try
            {
                command = MongoHelper.FromJson(typeof(Object), rawCommandJson) as IRequest;

                if (command == null)
                {
                    error = $"unity bridge command deserialize fail: {commandTypeName}";
                    return false;
                }

                command.RpcId = request.RpcId;
                commandTypeName = command.GetType().Name;
                return true;
            }
            catch (Exception e)
            {
                command = null;
                error = e.ToString();
                return false;
            }
        }

        public static async ETTask<IResponse> Dispatch(IRequest command)
        {
            try
            {
                if (!Registrations.TryGetValue(command.GetType(), out UnityBridgeHandlerRegistration registration))
                {
                    return UnityBridgeResponseHelper.CreateErrorResponse(
                        command?.RpcId ?? 0,
                        command?.GetType().Name,
                        UnityBridgeErrorCode.InvalidCommandLine,
                        $"unsupported unity bridge command type: {command?.GetType().FullName}");
                }

                IResponse response = await registration.Handler.Handle(command);
                if (response == null)
                {
                    return CreateErrorResponse(
                        command,
                        UnityBridgeErrorCode.HandlerFail,
                        $"unity bridge handler returned null: {registration.Handler.GetType().FullName}");
                }

                response.RpcId = command.RpcId;
                return response;
            }
            catch (Exception e)
            {
                return CreateErrorResponse(command, UnityBridgeErrorCode.HandlerFail, e.ToString());
            }
        }

        public static IResponse CreateErrorResponse(int rpcId, string commandType, int error, string message)
        {
            return UnityBridgeResponseHelper.CreateErrorResponse(rpcId, commandType, error, message);
        }

        public static IResponse CreateErrorResponse(IRequest command, int error, string message)
        {
            return CreateErrorResponse(command?.RpcId ?? 0, command?.GetType().Name, error, message);
        }

        public static Type GetResponseType(IRequest command)
        {
            return command != null && Registrations.TryGetValue(command.GetType(), out UnityBridgeHandlerRegistration registration)
                    ? registration.ResponseType
                    : typeof(ErrorResponse);
        }

        public static bool TryGetHandler(IRequest command, out IUnityBridgeHandler handler)
        {
            handler = null;
            if (command == null)
            {
                return false;
            }

            if (!Registrations.TryGetValue(command.GetType(), out UnityBridgeHandlerRegistration registration))
            {
                return false;
            }

            handler = registration.Handler;
            return handler != null;
        }

        public static bool TryDeserializePersistedCommand(string commandJson, out IRequest command, out string error)
        {
            command = null;
            error = null;

            EnsureMongoRegistered();

            if (!TryNormalizeCommandJson(
                    commandJson,
                    RegistrationsByName,
                    out string rawCommandJson,
                    out string commandTypeName,
                    out UnityBridgeHandlerRegistration _,
                    out error))
            {
                return false;
            }

            try
            {
                command = MongoHelper.FromJson(typeof(Object), rawCommandJson) as IRequest;
                if (command == null)
                {
                    error = $"unity bridge command deserialize fail: {commandTypeName}";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                command = null;
                error = e.ToString();
                return false;
            }
        }

        public static string GetPersistedCommandTypeName(string commandJson)
        {
            EnsureMongoRegistered();

            try
            {
                BsonDocument rootDocument = BsonDocument.Parse(commandJson);
                if (!TryGetCommandTypeName(rootDocument, out string commandTypeName))
                {
                    return string.Empty;
                }

                if (TryGetRegistration(commandTypeName, RegistrationsByName, out UnityBridgeHandlerRegistration registration))
                {
                    return registration.RequestType.Name;
                }

                return commandTypeName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Dictionary<Type, UnityBridgeHandlerRegistration> BuildRegistrations()
        {
            Dictionary<Type, UnityBridgeHandlerRegistration> registrations = new();
            foreach (Type handlerType in TypeCache.GetTypesDerivedFrom<IUnityBridgeHandler>())
            {
                if (handlerType.IsAbstract || handlerType.IsInterface)
                {
                    continue;
                }

                if (Activator.CreateInstance(handlerType) is not IUnityBridgeHandler handler)
                {
                    throw new Exception($"unity bridge handler invalid: {handlerType.FullName}");
                }

                if (handler.RequestType == null || !typeof(IRequest).IsAssignableFrom(handler.RequestType))
                {
                    throw new Exception($"unity bridge command type invalid: {handlerType.FullName}");
                }

                if (handler.ResponseType == null || !typeof(IResponse).IsAssignableFrom(handler.ResponseType))
                {
                    throw new Exception($"unity bridge response type invalid: {handlerType.FullName}");
                }

                if (registrations.ContainsKey(handler.RequestType))
                {
                    throw new Exception($"duplicate unity bridge handler: {handler.RequestType.FullName}");
                }

                registrations.Add(handler.RequestType, new UnityBridgeHandlerRegistration
                {
                    RequestType = handler.RequestType,
                    ResponseType = handler.ResponseType,
                    Handler = handler
                });
            }

            return registrations;
        }

        private static Dictionary<string, UnityBridgeHandlerRegistration> BuildRegistrationsByName(
                Dictionary<Type, UnityBridgeHandlerRegistration> registrations)
        {
            Dictionary<string, UnityBridgeHandlerRegistration> registrationsByName = new(StringComparer.Ordinal);
            foreach ((Type commandType, UnityBridgeHandlerRegistration registration) in registrations)
            {
                registrationsByName[commandType.Name] = registration;
                if (!string.IsNullOrWhiteSpace(commandType.FullName))
                {
                    registrationsByName[commandType.FullName] = registration;
                }
            }

            return registrationsByName;
        }

        private static bool TryGetRegistration(
                string commandTypeName,
                Dictionary<string, UnityBridgeHandlerRegistration> registrationsByName,
                out UnityBridgeHandlerRegistration registration)
        {
            registration = null;
            if (string.IsNullOrWhiteSpace(commandTypeName))
            {
                return false;
            }

            return registrationsByName.TryGetValue(commandTypeName, out registration);
        }

        private static bool TryNormalizeCommandJson(string commandJson,
                Dictionary<string, UnityBridgeHandlerRegistration> registrationsByName,
                out string rawCommandJson,
                out string commandTypeName,
                out UnityBridgeHandlerRegistration registration,
                out string error)
        {
            rawCommandJson = null;
            commandTypeName = string.Empty;
            registration = null;
            error = null;

            if (string.IsNullOrWhiteSpace(commandJson))
            {
                error = "unity bridge request json is empty";
                return false;
            }

            try
            {
                BsonDocument rootDocument = BsonDocument.Parse(commandJson);
                if (rootDocument.TryGetValue("CommandType", out _) || rootDocument.TryGetValue("Payload", out _))
                {
                    error = "unity bridge request json no longer supports CommandType/Payload, use _t";
                    return false;
                }

                if (!TryGetCommandTypeName(rootDocument, out commandTypeName))
                {
                    error = "unity bridge request json missing _t";
                    return false;
                }

                if (!TryGetRegistration(commandTypeName, registrationsByName, out registration))
                {
                    error = $"unsupported unity bridge command type: {commandTypeName}";
                    return false;
                }

                commandTypeName = registration.RequestType.Name;
                rawCommandJson = BuildDiscriminatedCommandJson(rootDocument, registration.RequestType);
                return true;
            }
            catch (Exception e)
            {
                error = e.ToString();
                return false;
            }
        }

        private static bool TryGetCommandTypeName(BsonDocument rootDocument, out string commandTypeName)
        {
            commandTypeName = string.Empty;
            if (rootDocument.TryGetValue("_t", out BsonValue typeValue))
            {
                switch (typeValue)
                {
                    case BsonString bsonString when !string.IsNullOrWhiteSpace(bsonString.Value):
                        commandTypeName = bsonString.Value;
                        return true;
                    case BsonArray bsonArray:
                        for (int i = bsonArray.Count - 1; i >= 0; --i)
                        {
                            if (bsonArray[i] is not BsonString item || string.IsNullOrWhiteSpace(item.Value))
                            {
                                continue;
                            }

                            commandTypeName = item.Value;
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        private static string BuildDiscriminatedCommandJson(BsonDocument payloadDocument, Type commandType)
        {
            BsonDocument normalizedDocument = new()
            {
                { "_t", commandType.FullName }
            };

            foreach (BsonElement element in payloadDocument)
            {
                if (string.Equals(element.Name, "_t", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                normalizedDocument.Add(element.Name, element.Value);
            }

            return normalizedDocument.ToJson();
        }

        private static void EnsureMongoRegistered()
        {
            if (mongoInitialized)
            {
                return;
            }

            MongoRegister.Init();
            mongoInitialized = true;
        }
    }
}
