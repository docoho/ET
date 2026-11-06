using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理同进程业务场景（<see cref="ServiceDiscoveryProxy"/>）取消服务变更订阅请求的处理器。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_UnsubscribeForwardHandler :
        MessageHandler<Scene, ServiceUnsubscribeRequest, ServiceUnsubscribeResponse>
    {
        /// <summary>
        /// 处理同进程业务场景取消订阅请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceUnsubscribeRequest request, ServiceUnsubscribeResponse response)
        {
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceUnsubscribeRequest),
                    nameof(request.SceneName), out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            string sceneName = scene.Name;
            try
            {
                if (agent == null)
                {
                    ServiceDiscoveryErrorHelper.SetInternalFailure(response, "service discovery agent missing");
                    return;
                }

                // 清理该场景在 Agent 侧登记的所有订阅过滤条件与 ActorId
                agent.UnsubscribeProxyServiceChange(request.SceneName);
                await ETTask.CompletedTask;
            }
            catch (Exception e)
            {
                ServiceDiscoveryErrorHelper.SetInternalFailure(response, e.Message);
                Log.Warning($"ServiceDiscovery agent unsubscribe forward failed scene: {sceneName} error: {e.Message}");
            }
        }
    }
}
