using System;

namespace ET.Server
{
    /// <summary>
    /// 处理业务场景 <see cref="ServiceDiscoveryProxy"/> 销毁时发来的注销消息（<see cref="ServiceProxyDestroyUnregisterMessage"/>）。
    /// 当业务 Scene 销毁时，Proxy 无法等待异步 RPC，因此通过单向消息通知同进程的 <see cref="ServiceDiscoveryAgent"/> 清理订阅、移除本地服务并后台同步给主节点。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_DestroyUnregisterMessageHandler:
        MessageHandler<Scene, ServiceProxyDestroyUnregisterMessage>
    {
        /// <summary>
        /// 执行 Proxy 销毁后的本地服务清理与后台注销调度。
        /// </summary>
        /// <param name="scene">ServiceDiscoveryAgent 场景</param>
        /// <param name="message">Proxy 销毁注销消息</param>
        protected override async ETTask Run(Scene scene, ServiceProxyDestroyUnregisterMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.SceneName))
            {
                Log.Warning($"{nameof(ServiceProxyDestroyUnregisterMessage)} invalid: SceneName is empty.");
                return;
            }

            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            if (agent == null)
            {
                Log.Warning(
                    $"ServiceDiscovery agent destroy unregister ignored because agent component is null scene: {scene.Name}");
                return;
            }

            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            string sceneName = scene.Name;
            string proxySceneName = message.SceneName;
            try
            {
                agent = agentRef;
                if (agent == null)
                {
                    return;
                }

                // Destroy阶段不再接收订阅通知，先从Agent侧移除订阅关系。
                agent.UnsubscribeProxyServiceChange(proxySceneName);

                // 从本地服务表移除，并标记 Dirty 触发后台向 Master 增量同步注销
                agent.RemoveLocalServiceAfterUnregister(proxySceneName);
                // 移除对该 Proxy 场景的心跳跟踪
                ServiceDiscoveryAgentProxyHeartbeat proxyHeartbeat = agent.GetComponent<ServiceDiscoveryAgentProxyHeartbeat>();
                proxyHeartbeat?.RemoveProxyHeartbeat(proxySceneName);

                await ETTask.CompletedTask;
            }
            catch (Exception e)
            {
                agent = agentRef;
                if (agent != null)
                {
                    Log.Warning(
                        $"ServiceDiscovery agent destroy unregister failed scene: {sceneName} proxyScene: {proxySceneName} error: {e.Message}");
                }
            }
        }
    }
}
