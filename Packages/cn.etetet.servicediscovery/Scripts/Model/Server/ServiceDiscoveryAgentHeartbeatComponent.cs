using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现中心主节点维护各进程 Agent 心跳状态的组件。
    /// 按进程内网地址（<see cref="Address"/>）聚合记录最后心跳时间。
    /// </summary>
    [ComponentOf(typeof(ServiceDiscovery))]
    public class ServiceDiscoveryAgentHeartbeatComponent : Entity, IAwake
    {
#if !UNITY_EDITOR
        /// <summary>Agent 心跳超时阈值（毫秒，非编辑器默认 10 秒）</summary>
        public long HeartbeatTimeout = 10 * 1000;
#else
        /// <summary>编辑器调试模式下的 Agent 心跳超时阈值（防止断点调试触发误摘除）</summary>
        public long HeartbeatTimeout = 10000 * 1000;
#endif

        /// <summary>主节点巡检 Agent 心跳超时的间隔（毫秒）</summary>
        public long HeartbeatCheckInterval = 1 * 1000;

        /// <summary>各进程地址（Key）对应的最后一次收到心跳或服务更新的单调时间戳（Value）</summary>
        public Dictionary<Address, long> AgentHeartbeatTimes = new();
    }
}
