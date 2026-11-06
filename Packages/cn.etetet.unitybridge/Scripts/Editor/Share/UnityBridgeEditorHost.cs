using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 编辑器常驻宿主（通过 [InitializeOnLoad] 随编辑器启动挂载到 EditorApplication.update），定期轮询 Temp/UnityBridge/requests 目录处理外部 CLI 命令。
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityBridgeEditorHost
    {
        private const double PollIntervalSeconds = 0.2d;

        private static double nextPollTime;
        private static bool isProcessingRequest;
        private static readonly string cachedRoot;

        static UnityBridgeEditorHost()
        {
            MongoRegister.Init();
            cachedRoot = UnityBridgePathHelper.ResolveRoot();
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextPollTime = 0d;
        }

        private static void Update()
        {
            string root = cachedRoot;
            double now = EditorApplication.timeSinceStartup;

            if (UnityBridgeDeferredRuntime.TryPump(root))
            {
                return;
            }

            if (now < nextPollTime)
            {
                return;
            }

            nextPollTime = now + PollIntervalSeconds;
            if (isProcessingRequest)
            {
                return;
            }

            ProcessOneRequestAsync(root).Coroutine();
        }

        private static async ETTask ProcessOneRequestAsync(string root)
        {
            isProcessingRequest = true;
            try
            {
                if (!UnityBridgeFileStore.TryTakeNextRequest(root, out string processingPath, out UnityBridgeRequestEnvelope request, out string readError))
                {
                    return;
                }

                if (request == null)
                {
                    Debug.LogError($"unity bridge request read fail: {readError}");
                    UnityBridgeFileStore.MoveToDeadletter(root, processingPath);
                    return;
                }

                HandleRequestResult handleResult = await HandleRequest(root, processingPath, request);
                if (handleResult.IsDeferred)
                {
                    return;
                }

                UnityBridgeFileStore.WriteResponse(root, request.RpcId, handleResult.ResponseJson);
                UnityBridgeFileStore.DeleteProcessing(processingPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                isProcessingRequest = false;
            }
        }

        private static async ETTask<HandleRequestResult> HandleRequest(string root, string processingPath, UnityBridgeRequestEnvelope request)
        {
            if (!UnityBridgeEditorDispatcher.TryParse(request, out IRequest command, out string commandTypeName, out string parseError))
            {
                return HandleRequestResult.FromResponse(
                    UnityBridgeMongoJsonHelper.ToJson(
                        UnityBridgeEditorDispatcher.CreateErrorResponse(request.RpcId, commandTypeName, UnityBridgeErrorCode.InvalidCommandLine, parseError)));
            }

            request.TimeoutMs = ResolveTimeoutMs(request.TimeoutMs, command);

            if (UnityBridgeFileStore.TryReadCachedResponse(root, request.IdempotencyKey, out string cachedResponseJson))
            {
                return HandleRequestResult.FromResponse(CloneResponseForRequest(request, command, cachedResponseJson));
            }

            if (request.TimeoutMs > 0)
            {
                long ageMs = (long)(DateTime.UtcNow - File.GetLastWriteTimeUtc(processingPath)).TotalMilliseconds;
                if (ageMs > request.TimeoutMs)
                {
                    return HandleRequestResult.FromResponse(
                        UnityBridgeMongoJsonHelper.ToJson(
                            UnityBridgeEditorDispatcher.CreateErrorResponse(command, UnityBridgeErrorCode.Timeout, "unity bridge request timeout")));
                }
            }

            IResponse response;
            using (UnityBridgeDeferredRuntime.EnterRequestScope(request))
            {
                response = await UnityBridgeEditorDispatcher.Dispatch(command);
            }

            if (UnityBridgeResponseHelper.IsDeferredResponse(response))
            {
                return HandleRequestResult.Deferred();
            }

            string responseJson = UnityBridgeMongoJsonHelper.ToJson(response);
            UnityBridgeFileStore.WriteCachedResponse(root, request.IdempotencyKey, responseJson);
            return HandleRequestResult.FromResponse(responseJson);
        }

        private static int ResolveTimeoutMs(int timeoutMs, IRequest command)
        {
            if (timeoutMs > 0)
            {
                return timeoutMs;
            }

            return command switch
            {
                Compile => 180000,
                Refresh or RegenProject or EnterPlay or ExitPlay => 60000,
                _ => 10000
            };
        }

        private static string CloneResponseForRequest(UnityBridgeRequestEnvelope request, IRequest command, string cachedResponseJson)
        {
            Type responseType = UnityBridgeEditorDispatcher.GetResponseType(command);
            if (!TryDeserializeCachedResponse(responseType, cachedResponseJson, out IResponse cachedResponse))
            {
                return UnityBridgeMongoJsonHelper.ToJson(
                    UnityBridgeEditorDispatcher.CreateErrorResponse(command, UnityBridgeErrorCode.HandlerFail, "unity bridge cached response deserialize fail"));
            }

            cachedResponse.RpcId = request.RpcId;
            return UnityBridgeMongoJsonHelper.ToJson(cachedResponse);
        }

        private static bool TryDeserializeCachedResponse(Type responseType, string cachedResponseJson, out IResponse cachedResponse)
        {
            cachedResponse = null;

            try
            {
                if (responseType != null && MongoHelper.FromJson(responseType, cachedResponseJson) is IResponse typedResponse)
                {
                    cachedResponse = typedResponse;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"unity bridge cached response deserialize as {responseType?.Name} failed: {e.Message}");
            }

            try
            {
                if (MongoHelper.FromJson(typeof(ErrorResponse), cachedResponseJson) is IResponse errorResponse)
                {
                    cachedResponse = errorResponse;
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"unity bridge cached response deserialize as ErrorResponse failed: {e.Message}");
            }

            return false;
        }

        private readonly struct HandleRequestResult
        {
            public string ResponseJson { get; }

            public bool IsDeferred { get; }

            private HandleRequestResult(string responseJson, bool isDeferred)
            {
                this.ResponseJson = responseJson;
                this.IsDeferred = isDeferred;
            }

            public static HandleRequestResult Deferred()
            {
                return new HandleRequestResult(null, true);
            }

            public static HandleRequestResult FromResponse(string responseJson)
            {
                return new HandleRequestResult(responseJson, false);
            }
        }
    }
}
