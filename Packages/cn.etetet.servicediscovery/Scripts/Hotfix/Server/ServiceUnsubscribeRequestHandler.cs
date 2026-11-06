namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（<see cref="SceneType.ServiceDiscovery"/>）拒绝直接取消订阅请求（<see cref="ServiceUnsubscribeRequest"/>）的守卫处理器。
    /// 架构约束：取消订阅必须在进程级 <see cref="SceneType.ServiceDiscoveryAgent"/> 侧完成。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnsubscribeRequestHandler : MessageHandler<Scene, ServiceUnsubscribeRequest, ServiceUnsubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnsubscribeRequest request, ServiceUnsubscribeResponse response)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryInvalidArgument;
            response.Message = "direct unsubscribe on ServiceDiscovery is not supported; use ServiceDiscoveryAgent";
            await ETTask.CompletedTask;
        }
    }
}
