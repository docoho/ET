using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现代理组件，挂载在Fiber上提供服务注册/订阅/查询入口
    /// 通过此组件统一转发到进程级 ServiceDiscoveryAgent
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public partial class ServiceDiscoveryProxy : Entity, IAwake, IDestroy
    {
        // 这里可以指定索引
        /// <summary>Proxy 本地缓存建立二级索引的元数据字段名列表</summary>
        public readonly string[] Indexs = { ServiceMetaKey.SceneType };
        
        /// <summary>
        /// 进程级 ServiceDiscoveryAgent 的 FiberInstanceId。
        /// </summary>
        public FiberInstanceId AgentFiberInstanceId;

        /// <summary>
        /// 缓存的服务列表，Key为SceneName，Value为对应的ServiceInfo实体引用
        /// </summary>
        public Dictionary<string, EntityRef<ServiceInfo>> SceneNameServices = new();
        //  索引,key1例如"SceneType",  key2 是值，例如SceneType的值，  HashSet中是ServiceCacheInfo的SceneName
        /// <summary>Proxy 本地服务二级倒排索引表</summary>
        public Dictionary<string, MultiMapSet<string, string>> ServicesIndexs = new();

        /// <summary>
        /// 订阅过滤条件缓存（用于本地保留调用参数）。
        /// </summary>
        public Dictionary<string, StringKV> SubscribeFilters = new();

        private EntityRef<ProcessInnerSender> processInnerSender;

        /// <summary>当前 Proxy 所属根 Scene 的名称（缓存供 Destroy 阶段发送注销消息使用）</summary>
        public string RootName;

        /// <summary>按 SceneType 解析目标服务时的最大重试次数</summary>
        public int ServiceResolveRetryTimes;

        /// <summary>按 SceneType 解析目标服务重试时的等待间隔（毫秒）</summary>
        public int ServiceResolveRetryIntervalMs;

        /// <summary>同进程内通信发送器组件引用</summary>
        public ProcessInnerSender ProcessInnerSender
        {
            get
            {
                return this.processInnerSender;
            }
            set
            {
                this.processInnerSender = value;
            }
        }
    }

}
