using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 进程级服务发现 Agent 运行状态枚举（支持按位组合）。
    /// </summary>
    [Flags]
    public enum ServiceDiscoveryAgentStatus
    {
        /// <summary>初始空闲状态</summary>
        None = 0,
        /// <summary>正在后台解析主节点并尝试全量注册中</summary>
        Bootstrapping = 1 << 0,
        /// <summary>已完成向当前活跃主节点的全量注册</summary>
        AgentRegistered = 1 << 1,
        /// <summary>正在后台向主节点同步增量服务变更（DirtyPublishedScenes）</summary>
        MutationSyncing = 1 << 2,
    }

    /// <summary>
    /// 进程级服务发现 Agent（每进程唯一）。
    /// 负责将业务 Fiber 的注册/订阅/查询请求转发到当前可用主节点，并处理主切换重试。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public partial class ServiceDiscoveryAgent : Entity, IAwake, IDestroy
    {
        // 这里可以指定索引
        /// <summary>本地服务缓存建立二级倒排索引的元数据字段名列表</summary>
        public readonly string[] Indexs = { ServiceMetaKey.SceneType };

        /// <summary>
        /// 本地服务缓存（由注册响应全量快照 + 变更通知维护）。
        /// </summary>
        public Dictionary<string, EntityRef<ServiceInfo>> SceneNameServices = new();
        // 索引,key1例如"SceneType", key2是值，HashSet中是ServiceInfo的SceneName
        /// <summary>本地服务二级倒排索引表</summary>
        public Dictionary<string, MultiMapSet<string, string>> ServicesIndexs = new();

        /// <summary>
        /// 本进程已发布到服务发现的本地服务快照。
        /// 仅表示“本地期望状态”，不等同于当前主节点返回的全量集群视图。
        /// </summary>
        public Dictionary<string, (ActorId ActorId, StringKV Metadata)> LocalPublishedServices = new();

        /// <summary>
        /// 本进程订阅者路由信息，Key为Proxy场景名，Value为订阅通知目标ActorId。
        /// </summary>
        public Dictionary<string, ActorId> ProxySubscribers = new();

        /// <summary>
        /// 本进程订阅者过滤条件，Key为Proxy场景名，SubKey为过滤器名。
        /// </summary>
        public MultiDictionary<string, string, StringKV> ProxySubscriberFilters = new();

        /// <summary>
        /// 当前活跃服务发现 ActorId。
        /// </summary>
        public ActorId ServiceDiscoveryActorId;

        /// <summary>
        /// 当前识别到的主机任期版本号（用于屏蔽过期主记录）。
        /// </summary>
        public long CurrentMasterEpoch;

        /// <summary>
        /// Agent 运行状态（初始化/注册状态/全量订阅状态）。
        /// </summary>
        public ServiceDiscoveryAgentStatus Status;

        /// <summary>
        /// 待同步到主节点的本地服务场景名集合。
        /// </summary>
        public HashSet<string> DirtyPublishedScenes = new();

        /// <summary>
        /// 按 scene 记录本地服务变更版本，用于增量同步去重。
        /// </summary>
        public Dictionary<string, int> PublishedSceneVersions = new();

        private EntityRef<MessageSender> messageSender;

        /// <summary>跨进程 Actor 消息发送器组件引用</summary>
        public MessageSender MessageSender
        {
            get
            {
                return this.messageSender;
            }
            set
            {
                this.messageSender = value;
            }
        }
    }
}
