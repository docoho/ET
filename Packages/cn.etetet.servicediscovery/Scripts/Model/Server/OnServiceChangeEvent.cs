using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务新增或更新时在业务场景（<see cref="ServiceDiscoveryProxy"/> 所在 Scene）发布的事件。
    /// </summary>
    public struct OnServiceChangeAddService
    {
        /// <summary>发生新增或更新的服务场景名</summary>
        public string ServiceName;
    }

    /// <summary>
    /// 服务下线移除时在业务场景（<see cref="ServiceDiscoveryProxy"/> 所在 Scene）发布的事件。
    /// </summary>
    public struct OnServiceChangeRemoveService
    {
        /// <summary>被移除的服务场景名</summary>
        public string ServiceName;
    }

    /// <summary>
    /// 测试用组件：统计各服务新增事件的触发次数。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscoveryTestAddEventCounterComponent : Entity, IAwake
    {
        /// <summary>按服务名称统计的 <see cref="OnServiceChangeAddService"/> 事件触发次数</summary>
        public Dictionary<string, int> Counts = new();
    }
}
