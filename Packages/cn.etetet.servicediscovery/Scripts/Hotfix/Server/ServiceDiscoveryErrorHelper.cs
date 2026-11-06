namespace ET.Server
{
    /// <summary>
    /// 服务发现错误码辅助类。
    /// 统一封装各类异常响应的状态码填充逻辑，以及判断某个 RPC 错误码是否需要触发主备故障转移（Failover）。
    /// </summary>
    public static class ServiceDiscoveryErrorHelper
    {
        /// <summary>
        /// 设置参数非法错误（<see cref="ErrorCode.ERR_ServiceDiscoveryInvalidArgument"/>）。
        /// </summary>
        public static void SetInvalidArgument(IResponse response, string message)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryInvalidArgument;
            response.Message = message;
        }

        /// <summary>
        /// 设置持久化失败错误（<see cref="ErrorCode.ERR_ServiceDiscoveryPersistenceFailed"/>）。
        /// </summary>
        public static void SetPersistenceFailed(IResponse response, string message)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryPersistenceFailed;
            response.Message = message;
        }

        /// <summary>
        /// 设置内部操作失败错误（<see cref="ErrorCode.ERR_ServiceDiscoveryOperationFailed"/>）。
        /// </summary>
        public static void SetInternalFailure(IResponse response, string message)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryOperationFailed;
            response.Message = message;
        }

        /// <summary>
        /// 设置主节点不可用错误（<see cref="ErrorCode.ERR_ServiceDiscoveryMasterUnavailable"/>）。
        /// </summary>
        public static void SetMasterUnavailable(IResponse response, string message)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryMasterUnavailable;
            response.Message = message;
        }

        /// <summary>
        /// 设置备节点拒绝写入错误（<see cref="ErrorCode.ERR_ServiceDiscoveryFollowerRejected"/>），并在消息中附带已知的主节点名称。
        /// </summary>
        public static void SetFollowerRejected(IResponse response, ServiceDiscovery serviceDiscovery)
        {
            string currentMaster = serviceDiscovery?.GetOrAddLease().CurrentMasterSceneName;
            response.Error = ErrorCode.ERR_ServiceDiscoveryFollowerRejected;
            response.Message = $"service discovery is follower currentMaster: {currentMaster}";
        }

        /// <summary>
        /// 当当前 ServiceDiscovery 节点不是可写主节点时，根据是否已知其他活跃主节点设置 <see cref="SetFollowerRejected"/> 或 <see cref="SetMasterUnavailable"/>。
        /// </summary>
        public static void SetNotWritableMaster(IResponse response, ServiceDiscovery serviceDiscovery)
        {
            if (serviceDiscovery == null)
            {
                SetMasterUnavailable(response, "service discovery master unavailable");
                return;
            }

            if (!string.IsNullOrEmpty(serviceDiscovery.GetOrAddLease().CurrentMasterSceneName)
                || serviceDiscovery.GetOrAddLease().CurrentMasterActorId != default)
            {
                SetFollowerRejected(response, serviceDiscovery);
                return;
            }

            SetMasterUnavailable(response, "service discovery master unavailable");
        }

        /// <summary>
        /// 判断指定的错误码是否代表当前访问的 ServiceDiscovery 节点不可用或非主节点，从而需要触发重新解析 Master 的故障转移（Failover）。
        /// </summary>
        public static bool ShouldTriggerFailover(int error)
        {
            return error == ErrorCode.ERR_ServiceDiscoveryFollowerRejected
                   || error == ErrorCode.ERR_ServiceDiscoveryMasterUnavailable
                   || error == ErrorCode.ERR_NotFoundActor
                   || error == ErrorCode.ERR_RpcFail
                   || error == ErrorCode.ERR_MessageTimeout;
        }
    }
}
