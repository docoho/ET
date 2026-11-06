using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理同进程业务场景（<see cref="ServiceDiscoveryProxy"/>）发来的心跳请求处理器。
    /// 刷新对应 Proxy 场景在 Agent 侧的最后活跃时间戳，防止因心跳超时被判定失活注销。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_HeartbeatHandler :
        MessageHandler<Scene, ServiceHeartbeatRequest, ServiceHeartbeatResponse>
    {
        /// <summary>
        /// 处理同进程 Proxy 的心跳上报请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceHeartbeatRequest request, ServiceHeartbeatResponse response)
        {
            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            if (agent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 校验请求中的 SceneName 是否非空
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceHeartbeatRequest),
                    nameof(request.SceneName), out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                await ETTask.CompletedTask;
                return;
            }

            ServiceDiscoveryAgentProxyHeartbeat proxyHeartbeat = agent.GetComponent<ServiceDiscoveryAgentProxyHeartbeat>();
            if (proxyHeartbeat == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "proxy heartbeat component missing";
                return;
            }

            // 更新该业务 Scene 的心跳时间戳
            proxyHeartbeat.TouchProxyHeartbeat(request.SceneName);
            await ETTask.CompletedTask;
        }
    }
}
