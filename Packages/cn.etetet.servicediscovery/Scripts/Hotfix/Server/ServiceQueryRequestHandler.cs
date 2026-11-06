namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（<see cref="SceneType.ServiceDiscovery"/>）拒绝直接服务查询请求（<see cref="ServiceQueryRequest"/>）的守卫处理器。
    /// 架构约束：所有服务查询必须在各进程本地的 <see cref="SceneType.ServiceDiscoveryAgent"/> 缓存上完成，禁止跨进程直接查询主节点。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceQueryRequestHandler : MessageHandler<Scene, ServiceQueryRequest, ServiceQueryResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceQueryRequest request, ServiceQueryResponse response)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryInvalidArgument;
            response.Message = "direct query on ServiceDiscovery is not supported; use ServiceDiscoveryAgent";
            await ETTask.CompletedTask;
        }
    }
}
