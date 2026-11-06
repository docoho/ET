using System;

namespace ET
{
    /// <summary>
    /// UnityBridge 延迟命令（Deferred Command）运行时调度器，负责跨 Domain Reload 持久化挂起命令、轮询恢复执行以及超时收尾。
    /// </summary>
    internal static class UnityBridgeDeferredRuntime
    {
        private sealed class RequestScope : IDisposable
        {
            private readonly UnityBridgeRequestEnvelope previousRequest;

            public RequestScope(UnityBridgeRequestEnvelope request)
            {
                this.previousRequest = currentRequest;
                currentRequest = request;
            }

            public void Dispose()
            {
                currentRequest = this.previousRequest;
            }
        }

        private static bool isProcessingDeferred;
        private static UnityBridgeRequestEnvelope currentRequest;

        public static IDisposable EnterRequestScope(UnityBridgeRequestEnvelope request)
        {
            return new RequestScope(request);
        }

        public static bool TryPump(string root)
        {
            if (!UnityBridgeFileStore.TryReadPendingCommandState(root, out UnityBridgeDeferredCommandState pendingState, out string error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    UnityEngine.Debug.LogError(error);
                }

                return false;
            }

            if (pendingState == null)
            {
                return false;
            }

            if (isProcessingDeferred)
            {
                return true;
            }

            PumpAsync(root, pendingState).Coroutine();
            return true;
        }

        internal static bool TryCreatePendingCurrent(IRequest command, out IResponse errorResponse)
        {
            if (command == null)
            {
                errorResponse = UnityBridgeResponseHelper.CreateErrorResponse(
                    currentRequest?.RpcId ?? 0,
                    string.Empty,
                    UnityBridgeErrorCode.InvalidCommandLine,
                    "unity bridge deferred command is null");
                return false;
            }

            if (currentRequest == null)
            {
                errorResponse = UnityBridgeResponseHelper.CreateErrorResponse(
                    command.RpcId,
                    command.GetType().Name,
                    UnityBridgeErrorCode.HandlerFail,
                    "unity bridge deferred request scope is missing");
                return false;
            }

            string root = UnityBridgePathHelper.ResolveRoot();

            if (UnityBridgeFileStore.TryReadPendingCommandState(root, out UnityBridgeDeferredCommandState existingState, out string readError))
            {
                string existingCommandType = UnityBridgeEditorDispatcher.GetPersistedCommandTypeName(existingState.CommandJson);
                errorResponse = UnityBridgeResponseHelper.CreateErrorResponse(
                    command.RpcId,
                    command.GetType().Name,
                    UnityBridgeErrorCode.HandlerFail,
                    $"deferred unity bridge command already pending: {existingCommandType}");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(readError))
            {
                errorResponse = UnityBridgeResponseHelper.CreateErrorResponse(
                    command.RpcId,
                    command.GetType().Name,
                    UnityBridgeErrorCode.HandlerFail,
                    readError);
                return false;
            }

            UnityBridgeDeferredCommandState pendingState = new()
            {
                CommandJson = UnityBridgeMongoJsonHelper.ToCommandJson(command),
                RpcId = currentRequest.RpcId,
                IdempotencyKey = currentRequest.IdempotencyKey,
                TimeoutMs = currentRequest.TimeoutMs,
                StartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            UnityBridgeFileStore.WritePendingCommandState(root, pendingState);
            errorResponse = null;
            return true;
        }

        internal static void RollbackCurrent()
        {
            string root = UnityBridgePathHelper.ResolveRoot();
            UnityBridgeFileStore.DeletePendingCommandState(root);
        }

        private static async ETTask PumpAsync(string root, UnityBridgeDeferredCommandState pendingState)
        {
            isProcessingDeferred = true;
            try
            {
                if (IsTimeout(pendingState))
                {
                    CompleteWithError(root, pendingState, UnityBridgeErrorCode.Timeout, "unity bridge request timeout");
                    return;
                }

                if (!UnityBridgeEditorDispatcher.TryDeserializePersistedCommand(pendingState.CommandJson, out IRequest deferredCommand, out string readCommandError))
                {
                    CompleteWithError(root, pendingState, UnityBridgeErrorCode.HandlerFail, readCommandError);
                    return;
                }

                deferredCommand.RpcId = pendingState.RpcId;

                if (!UnityBridgeEditorDispatcher.TryGetHandler(deferredCommand, out IUnityBridgeHandler handler))
                {
                    CompleteWithError(
                        root,
                        pendingState,
                        UnityBridgeErrorCode.HandlerFail,
                        $"unity bridge handler is missing: {deferredCommand.GetType().Name}");
                    return;
                }

                if (handler is not IUnityBridgeDeferredHandler deferredHandler)
                {
                    CompleteWithError(
                        root,
                        pendingState,
                        UnityBridgeErrorCode.HandlerFail,
                        $"unity bridge deferred handler is missing: {deferredCommand.GetType().Name}");
                    return;
                }

                IResponse response = await deferredHandler.Deferred(deferredCommand, pendingState.StartedAt);
                if (response == null)
                {
                    return;
                }

                response.RpcId = pendingState.RpcId;
                Complete(root, pendingState, UnityBridgeMongoJsonHelper.ToJson(response));
            }
            finally
            {
                isProcessingDeferred = false;
            }
        }

        private static bool IsTimeout(UnityBridgeDeferredCommandState pendingState)
        {
            if (pendingState.TimeoutMs <= 0)
            {
                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return now - pendingState.StartedAt > pendingState.TimeoutMs;
        }

        private static void CompleteWithError(string root, UnityBridgeDeferredCommandState pendingState, int error, string message)
        {
            string commandType = UnityBridgeEditorDispatcher.GetPersistedCommandTypeName(pendingState.CommandJson);
            string responseJson = UnityBridgeMongoJsonHelper.ToJson(
                UnityBridgeEditorDispatcher.CreateErrorResponse(pendingState.RpcId, commandType, error, message));
            Complete(root, pendingState, responseJson);
        }

        private static void Complete(string root, UnityBridgeDeferredCommandState pendingState, string responseJson)
        {
            UnityBridgeFileStore.WriteCachedResponse(root, pendingState.IdempotencyKey, responseJson);
            UnityBridgeFileStore.WriteResponse(root, pendingState.RpcId, responseJson);
            UnityBridgeFileStore.DeleteProcessing(UnityBridgeFileStore.GetProcessingPath(root, pendingState.RpcId));
            UnityBridgeFileStore.DeletePendingCommandState(root);
        }
    }
}
