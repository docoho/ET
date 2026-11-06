namespace ET.Server
{
    /// <summary>
    /// 服务变更通知（<see cref="ServiceChangeNotification"/>）消息处理器。
    /// 支持在全场景（<see cref="SceneType.All"/>）处理：
    /// 1. 当消息到达进程级 <see cref="ServiceDiscoveryAgent"/> 时，校验 Fencing Epoch 后更新进程本地服务缓存并向本进程内订阅了相关过滤条件的 Proxy 扇出通知；
    /// 2. 当消息到达业务场景的 <see cref="ServiceDiscoveryProxy"/> 时，更新业务场景本地的服务缓存并触发增删事件。
    /// </summary>
    [MessageHandler(SceneType.All)]
    public class ServiceChangeNotificationHandler : MessageHandler<Scene, ServiceChangeNotification>
    {
        /// <summary>
        /// 处理收到的服务变更通知消息。
        /// </summary>
        /// <param name="scene">当前接收消息的 Scene</param>
        /// <param name="message">服务变更通知消息（包含变更类型、Epoch、MasterActorId 和变更的服务列表）</param>
        protected override async ETTask Run(Scene scene, ServiceChangeNotification message)
        {
            // 若当前场景挂载了业务侧代理组件 ServiceDiscoveryProxy，则更新 Proxy 缓存并发布场景事件
            ServiceDiscoveryProxy proxy = scene.GetComponent<ServiceDiscoveryProxy>();
            if (proxy != null)
            {
                proxy.OnServiceChangeNotification(message.ChangeType, message.ServiceInfo);
            }

            // 若当前场景是进程级代理 ServiceDiscoveryAgent，则校验 Epoch 并扇出通知给本进程内的订阅者
            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            if (agent != null)
            {
                agent.OnServiceChangeNotification(message);
            }

            await ETTask.CompletedTask;
        }
    }
}
