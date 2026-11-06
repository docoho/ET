using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MongoDB.Bson;

namespace ET
{
    /// <summary>
    /// UnityBridge 命令行工具（ET.UnityBridge）主入口，负责将外部 JSON 命令写入本地桥接目录并轮询等待 Unity Editor 返回响应。
    /// </summary>
    internal static class Program
    {
        private static readonly HashSet<string> TransportOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            "--root",
            "--waitMs",
            "--timeoutMs",
            "--idempotencyKey"
        };

        private const int DefaultWaitMs = 15000;
        private const int DefaultDeferredWaitMs = 185000;

        /// <summary>
        /// 命令行入口，初始化 UnityBridgeAppHost 后解析命令参数并发送请求
        /// </summary>
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                new UnityBridgeAppHost().Start();

                if (args.Length == 0)
                {
                    Console.Error.WriteLine("unity bridge command is empty");
                    return 2;
                }

                return SendDirect(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static int SendDirect(string[] args)
        {
            int rpcId = GenerateRpcId();
            string root = null;
            int waitMs = DefaultWaitMs;
            int timeoutMs = 0;
            string idempotencyKey = null;
            string requestJson = args[0];
            bool hasExplicitWaitMs = false;

            for (int i = 1; i < args.Length; ++i)
            {
                string arg = args[i];
                if (!TransportOptions.Contains(arg))
                {
                    continue;
                }

                string nextValue = i + 1 < args.Length ? args[i + 1] : null;
                switch (arg)
                {
                    case "--root":
                        root = nextValue;
                        ++i;
                        continue;
                    case "--waitMs":
                        if (int.TryParse(nextValue, out int parsedWaitMs))
                        {
                            waitMs = parsedWaitMs;
                            hasExplicitWaitMs = true;
                        }

                        ++i;
                        continue;
                    case "--timeoutMs":
                        if (int.TryParse(nextValue, out int parsedTimeoutMs))
                        {
                            timeoutMs = parsedTimeoutMs;
                        }

                        ++i;
                        continue;
                    case "--idempotencyKey":
                        idempotencyKey = nextValue;
                        ++i;
                        continue;
                }
            }

            idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;
            string rootPath = UnityBridgePathHelper.ResolveRoot(root);

            UnityBridgeRequestEnvelope request = new()
            {
                RpcId = rpcId,
                IdempotencyKey = idempotencyKey,
                TimeoutMs = timeoutMs,
                CommandJson = requestJson
            };

            return SendRequest(rootPath, request, waitMs, hasExplicitWaitMs);
        }

        private static int SendRequest(string root, UnityBridgeRequestEnvelope request, int waitMs, bool hasExplicitWaitMs)
        {
            UnityBridgeFileStore.WriteRequest(root, request);

            long deadline = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + waitMs;
            bool hasAutoExtendedWait = false;
            while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() <= deadline)
            {
                if (UnityBridgeFileStore.TryReadResponse(root, request.RpcId, out string responseJson))
                {
                    UnityBridgeFileStore.DeleteResponse(root, request.RpcId);
                    Console.WriteLine(responseJson);
                    BsonDocument responseDocument = BsonDocument.Parse(responseJson);
                    if (responseDocument.TryGetValue("Error", out BsonValue errorValue))
                    {
                        return errorValue.ToInt32() == UnityBridgeErrorCode.Success ? 0 : 1;
                    }

                    return 1;
                }

                if (!hasExplicitWaitMs && !hasAutoExtendedWait && HasPendingDeferredRequest(root, request.RpcId))
                {
                    deadline = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + DefaultDeferredWaitMs;
                    hasAutoExtendedWait = true;
                    continue;
                }

                Thread.Sleep(100);
            }

            ErrorResponse timeoutResponse = ErrorResponse.Create();
            timeoutResponse.RpcId = request.RpcId;
            timeoutResponse.Error = UnityBridgeErrorCode.Timeout;
            timeoutResponse.Message = "wait unity bridge response timeout";
            timeoutResponse.Command = string.Empty;
            Console.WriteLine(UnityBridgeMongoJsonHelper.ToJson(timeoutResponse));
            return 1;
        }

        private static bool HasPendingDeferredRequest(string root, int rpcId)
        {
            string path = UnityBridgeFileStore.GetPendingCommandPath(root);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                BsonDocument document = BsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
                return document.TryGetValue("RpcId", out BsonValue rpcIdValue) && rpcIdValue.ToInt32() == rpcId;
            }
            catch
            {
                return false;
            }
        }

        private static int GenerateRpcId()
        {
            return unchecked(Environment.ProcessId ^ Random.Shared.Next(1, int.MaxValue) ^ (int)DateTime.UtcNow.Ticks);
        }
    }
}
