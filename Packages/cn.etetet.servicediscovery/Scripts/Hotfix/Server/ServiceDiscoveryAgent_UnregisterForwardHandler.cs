using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理同进程业务场景（<see cref="ServiceDiscoveryProxy"/>）主动注销服务请求的处理器。
    /// 移除本地服务缓存、取消该场景的订阅关系、清理心跳跟踪，并在后台异步将注销操作同步给主 ServiceDiscovery。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_UnregisterForwardHandler :
        MessageHandler<Scene, ServiceUnregisterRequest, ServiceUnregisterResponse>
    {
        /// <summary>
        /// 处理同进程业务场景的主动注销请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceUnregisterRequest request, ServiceUnregisterResponse response)
        {
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceUnregisterRequest),
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

                // 移除本地服务并通知同进程订阅者，同时调度后台增量注销同步
                agent.RemoveLocalServiceAfterUnregister(request.SceneName);
                // 移除该 Proxy 的所有订阅过滤条件
                agent.UnsubscribeProxyServiceChange(request.SceneName);

                // 移除对该 Proxy 场景的心跳监控
                ServiceDiscoveryAgentProxyHeartbeat proxyHeartbeat = agent.GetComponent<ServiceDiscoveryAgentProxyHeartbeat>();
                proxyHeartbeat?.RemoveProxyHeartbeat(request.SceneName);

                await ETTask.CompletedTask;
            }
            catch (Exception e)
            {
                ServiceDiscoveryErrorHelper.SetInternalFailure(response, e.Message);
                Log.Warning($"ServiceDiscovery agent unregister forward failed scene: {sceneName} error: {e.Message}");
            }
        }
    }
}
