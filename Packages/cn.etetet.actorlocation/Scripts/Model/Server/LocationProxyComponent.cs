using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 位置定位服务客户端代理组件（挂载在各业务 Scene 上，封装向主 Location 节点发起 Add/Remove/Lock/UnLock/Get 的 RPC 调用与重试逻辑）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class LocationProxyComponent: Entity, IAwake
    {
        /// <summary>
        /// 当前缓存的主 Location 场景名称。
        /// </summary>
        public string primaryLocationSceneName;

        /// <summary>
        /// 当前缓存的主 Location 节点优先级 ID（PriorityId）。
        /// </summary>
        public ulong primaryLocationPriorityId;

        /// <summary>
        /// Location RPC 请求失败时的最大重试次数。
        /// </summary>
        public int locationRequestRetryTimes;

        /// <summary>
        /// Location RPC 请求重试间隔时间（毫秒）。
        /// </summary>
        public int locationRequestRetryIntervalMs;
    }
}
