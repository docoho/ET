using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理同进程业务场景（<see cref="ServiceDiscoveryProxy"/>）服务注册请求的处理器。
    /// 采用本地优先与后台异步同步策略：先立即写入 Agent 本地发布表并通知本进程内的订阅者，再由后台协程异步同步到主 ServiceDiscovery。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_RegisterForwardHandler :
        MessageHandler<Scene, ServiceRegisterRequest, ServiceRegisterResponse>
    {
        /// <summary>
        /// 处理同进程业务场景的服务注册请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceRegisterRequest request, ServiceRegisterResponse response)
        {
            // 校验必填字段：SceneName、ActorId 与 Metadata
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceRegisterRequest),
                    nameof(request.SceneName), out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateRequiredActorId(request.ActorId, nameof(ServiceRegisterRequest),
                    nameof(request.ActorId), out errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(request.Metadata, nameof(ServiceRegisterRequest), nameof(request.Metadata),
                    out errorMessage))
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

                // 更新本地服务缓存、通知本地订阅者，并标记该场景为 Dirty 触发后台同步给 Master
                agent.UpsertLocalServiceAfterRegister(request.SceneName, request.ActorId, request.Metadata);

                // 强制添加并刷新该 Proxy 场景的心跳记录
                ServiceDiscoveryAgentProxyHeartbeat proxyHeartbeat = agent.GetComponent<ServiceDiscoveryAgentProxyHeartbeat>();
                proxyHeartbeat?.TouchProxyHeartbeat(request.SceneName, true);

                await ETTask.CompletedTask;
            }
            catch (Exception e)
            {
                ServiceDiscoveryErrorHelper.SetInternalFailure(response, e.Message);
                Log.Warning($"ServiceDiscovery agent register forward failed scene: {sceneName} error: {e.Message}");
            }
        }
    }
}
