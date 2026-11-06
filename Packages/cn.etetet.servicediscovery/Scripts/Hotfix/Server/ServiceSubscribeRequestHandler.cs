namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（<see cref="SceneType.ServiceDiscovery"/>）拒绝直接订阅请求（<see cref="ServiceSubscribeRequest"/>）的守卫处理器。
    /// 架构约束：业务场景必须向同进程的 <see cref="SceneType.ServiceDiscoveryAgent"/> 发起订阅，主节点仅按进程粒度向 Agent 推送变更。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceSubscribeRequestHandler : MessageHandler<Scene, ServiceSubscribeRequest, ServiceSubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceSubscribeRequest request, ServiceSubscribeResponse response)
        {
            response.Error = ErrorCode.ERR_ServiceDiscoveryInvalidArgument;
            response.Message = "direct subscribe on ServiceDiscovery is not supported; use ServiceDiscoveryAgent";
            await ETTask.CompletedTask;
        }
    }
}
