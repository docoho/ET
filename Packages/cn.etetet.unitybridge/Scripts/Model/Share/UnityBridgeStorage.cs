using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ET
{
    /// <summary>
    /// UnityBridge 目录常量与路径解析。
    /// </summary>
    public static class UnityBridgePathHelper
    {
        public const string RootEnvironmentVariable = "ET_UNITY_BRIDGE_ROOT";
        public const string DefaultRoot = "Temp/UnityBridge";

        public static string ResolveRoot(string root = null)
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                return Path.GetFullPath(root);
            }

            string envRoot = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envRoot))
            {
                return Path.GetFullPath(envRoot);
            }

            return Path.GetFullPath(DefaultRoot);
        }

        public static string GetRequestsDirectory(string root)
        {
            return Path.Combine(root, "requests");
        }

        public static string GetProcessingDirectory(string root)
        {
            return Path.Combine(root, "processing");
        }

        public static string GetResponsesDirectory(string root)
        {
            return Path.Combine(root, "responses");
        }

        public static string GetDeadletterDirectory(string root)
        {
            return Path.Combine(root, "deadletter");
        }

        public static string GetStateDirectory(string root)
        {
            return Path.Combine(root, "state");
        }

        public static string GetIdempotencyDirectory(string root)
        {
            return Path.Combine(GetStateDirectory(root), "idempotency");
        }
    }

    /// <summary>
    /// 文件存储帮助类。
    /// </summary>
    public static class UnityBridgeFileStore
    {
        public static void EnsureDirectories(string root)
        {
            Directory.CreateDirectory(UnityBridgePathHelper.GetRequestsDirectory(root));
            Directory.CreateDirectory(UnityBridgePathHelper.GetProcessingDirectory(root));
            Directory.CreateDirectory(UnityBridgePathHelper.GetResponsesDirectory(root));
            Directory.CreateDirectory(UnityBridgePathHelper.GetDeadletterDirectory(root));
            Directory.CreateDirectory(UnityBridgePathHelper.GetStateDirectory(root));
            Directory.CreateDirectory(UnityBridgePathHelper.GetIdempotencyDirectory(root));
        }

        public static string GetRequestPath(string root, int rpcId)
        {
            return Path.Combine(UnityBridgePathHelper.GetRequestsDirectory(root), $"{rpcId}.json");
        }

        public static string GetResponsePath(string root, int rpcId)
        {
            return Path.Combine(UnityBridgePathHelper.GetResponsesDirectory(root), $"{rpcId}.json");
        }

        public static string GetProcessingPath(string root, int rpcId)
        {
            return Path.Combine(UnityBridgePathHelper.GetProcessingDirectory(root), $"{rpcId}.json");
        }

        public static string GetPendingCommandPath(string root)
        {
            return Path.Combine(UnityBridgePathHelper.GetStateDirectory(root), "pending-command.json");
        }

        public static void WriteRequest(string root, UnityBridgeRequestEnvelope request)
        {
            EnsureDirectories(root);
            WriteTextAtomic(GetRequestPath(root, request.RpcId), UnityBridgeMongoJsonHelper.ToJson(request));
        }

        public static bool TryTakeNextRequest(string root, out string processingPath, out UnityBridgeRequestEnvelope request, out string error)
        {
            EnsureDirectories(root);
            processingPath = null;
            request = null;
            error = null;

            string[] requestFiles = Directory.GetFiles(UnityBridgePathHelper.GetRequestsDirectory(root), "*.json");
            Array.Sort(requestFiles, StringComparer.Ordinal);

            foreach (string requestPath in requestFiles)
            {
                string targetPath = Path.Combine(UnityBridgePathHelper.GetProcessingDirectory(root), Path.GetFileName(requestPath));
                try
                {
                    File.Move(requestPath, targetPath);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                processingPath = targetPath;
                try
                {
                    request = MongoHelper.FromJson<UnityBridgeRequestEnvelope>(File.ReadAllText(targetPath, Encoding.UTF8));
                }
                catch (Exception e)
                {
                    error = e.ToString();
                }

                return true;
            }

            return false;
        }

        public static void WriteResponse(string root, int rpcId, string responseJson)
        {
            EnsureDirectories(root);
            WriteTextAtomic(GetResponsePath(root, rpcId), responseJson);
        }

        public static bool TryReadResponse(string root, int rpcId, out string responseJson)
        {
            responseJson = null;
            string responsePath = GetResponsePath(root, rpcId);
            if (!File.Exists(responsePath))
            {
                return false;
            }

            responseJson = File.ReadAllText(responsePath, Encoding.UTF8);
            return true;
        }

        public static void DeleteResponse(string root, int rpcId)
        {
            string responsePath = GetResponsePath(root, rpcId);
            if (File.Exists(responsePath))
            {
                File.Delete(responsePath);
            }
        }

        public static bool TryReadCachedResponse(string root, string idempotencyKey, out string responseJson)
        {
            responseJson = null;
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return false;
            }

            string cachePath = GetIdempotencyPath(root, idempotencyKey);
            if (!File.Exists(cachePath))
            {
                return false;
            }

            responseJson = File.ReadAllText(cachePath, Encoding.UTF8);
            return true;
        }

        public static void WriteCachedResponse(string root, string idempotencyKey, string responseJson)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return;
            }

            EnsureDirectories(root);
            WriteTextAtomic(GetIdempotencyPath(root, idempotencyKey), responseJson);
        }

        public static bool TryReadPendingCommandState(string root, out UnityBridgeDeferredCommandState pendingState, out string error)
        {
            pendingState = null;
            error = null;
            string path = GetPendingCommandPath(root);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                pendingState = MongoHelper.FromJson<UnityBridgeDeferredCommandState>(File.ReadAllText(path, Encoding.UTF8));
                if (pendingState == null)
                {
                    error = "unity bridge deferred command state deserialize returned null";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = $"unity bridge deferred command state read fail: {e}";
                return false;
            }
        }

        public static void WritePendingCommandState(string root, UnityBridgeDeferredCommandState pendingState)
        {
            EnsureDirectories(root);
            WriteTextAtomic(GetPendingCommandPath(root), UnityBridgeMongoJsonHelper.ToJson(pendingState));
        }

        public static void DeletePendingCommandState(string root)
        {
            string path = GetPendingCommandPath(root);
            if (!File.Exists(path))
            {
                return;
            }

            File.Delete(path);
        }

        public static void DeleteProcessing(string processingPath)
        {
            if (string.IsNullOrWhiteSpace(processingPath) || !File.Exists(processingPath))
            {
                return;
            }

            File.Delete(processingPath);
        }

        public static void MoveToDeadletter(string root, string processingPath)
        {
            if (string.IsNullOrWhiteSpace(processingPath) || !File.Exists(processingPath))
            {
                return;
            }

            EnsureDirectories(root);
            string deadletterPath = Path.Combine(UnityBridgePathHelper.GetDeadletterDirectory(root), Path.GetFileName(processingPath));
            if (File.Exists(deadletterPath))
            {
                File.Delete(deadletterPath);
            }

            File.Move(processingPath, deadletterPath);
        }

        private static string GetIdempotencyPath(string root, string idempotencyKey)
        {
            byte[] rawBytes = Encoding.UTF8.GetBytes(idempotencyKey);
            byte[] hashBytes;
            using (SHA256 sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(rawBytes);
            }

            StringBuilder stringBuilder = new(hashBytes.Length * 2);
            foreach (byte hashByte in hashBytes)
            {
                stringBuilder.Append(hashByte.ToString("x2"));
            }

            return Path.Combine(UnityBridgePathHelper.GetIdempotencyDirectory(root), $"{stringBuilder}.json");
        }

        private static void WriteTextAtomic(string path, string content)
        {
            string tempPath = $"{path}.tmp";
            File.WriteAllText(tempPath, content ?? string.Empty, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
    }

    /// <summary>
    /// UnityBridge JSON 序列化辅助类，基于 MongoDB Bson 序列化器实现对象与带 _t 类型鉴别器的 JSON 互转。
    /// </summary>
    public static class UnityBridgeMongoJsonHelper
    {
        public static string ToJson(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            Type actualType = value.GetType();
            BsonSerializationArgs args = new(actualType, false, false);
            return BsonExtensionMethods.ToJson(value, actualType, MongoHelper.ConfigSettings, null, null, args);
        }

        public static string ToCommandJson(IRequest request)
        {
            if (request == null)
            {
                return string.Empty;
            }

            string json = ToJson(request);
            BsonDocument document = string.IsNullOrWhiteSpace(json)
                    ? new BsonDocument()
                    : BsonDocument.Parse(json);
            document["_t"] = request.GetType().FullName;
            return document.ToJson();
        }
    }
}
