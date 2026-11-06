using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现中心主节点的服务变更通知去重与批处理缓冲组件（挂载于 <see cref="ServiceDiscovery"/>）。
    /// 将短时间内的频繁服务增删按目标 Agent 和 `ChangeType:SceneName` 合并去重，并按固定防抖周期和批次上限发送，防止大规模启停时的广播风暴。
    /// </summary>
    [ComponentOf(typeof(ServiceDiscovery))]
    public class ServiceDiscoveryNotificationBufferComponent : Entity, IAwake
    {
        /// <summary>通知发送防抖间隔（毫秒，默认 100ms）</summary>
        public long NotificationDebounceInterval = 100;

        /// <summary>单条 <see cref="ServiceChangeNotification"/> 消息最多包含的服务条目数</summary>
        public int NotificationBatchMaxItems = 128;

        /// <summary>上次刷出通知缓冲区的单调时间戳</summary>
        public long LastNotificationFlushTime;

        /// <summary>按目标 Agent ActorId 分组的待发送通知去重字典</summary>
        public Dictionary<ActorId, Dictionary<string, (int ChangeType, string SceneName, ActorId ActorId, StringKV Metadata)>>
            PendingNotifications = new();
    }
}
