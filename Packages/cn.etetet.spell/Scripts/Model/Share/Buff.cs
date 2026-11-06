using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// Buff 实体，挂载在 BuffComponent 下。在 ET10 的“技能即 Buff”架构中，主动技能释放和持续性状态均表现为 Buff。
    /// </summary>
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<int>, IDestroy
    {
        /// <summary>
        /// 对应的 BuffConfig 配置表 ID（若为主技能 Buff，则与 SpellConfig.Id 相同）。
        /// </summary>
        public int ConfigId { get; set; }
        /// <summary>
        /// 施法者 Unit 的实体 ID。
        /// </summary>
        public long Caster { get; set; }
        /// <summary>
        /// Buff 创建时间戳（毫秒）。
        /// </summary>
        public long CreateTime { get; set; }
        /// <summary>
        /// 周期性触发（Tick）的间隔时间（毫秒）。
        /// </summary>
        public int TickTime { get; set; }
        /// <summary>
        /// Buff 过期时间戳（毫秒）。
        /// </summary>
        public long ExpireTime { get; set; }
        /// <summary>
        /// 当前叠加层数。
        /// </summary>
        public int Stack { get; set; }
        /// <summary>
        /// 超时自动移除定时器的 ID。
        /// </summary>
        public long TimeoutTimer;

        /// <summary>
        /// 关联的 Buff 上下文数据组件引用（即使 Buff 本身销毁，上下文也可在异步流程中安全读取）。
        /// </summary>
        [BsonIgnore]
        public EntityRef<BuffData> BuffData;
    }
}