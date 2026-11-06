using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Buff 子 Buff 管理组件，记录由当前父 Buff 派生出的子 Buff 引用，当父 Buff 移除时可级联移除带有 ParentRemoveRemove 标记的子 Buff。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffChildrenComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 派生出的子 Buff 实体弱引用列表。
        /// </summary>
        public List<EntityRef<Buff>> Buffs { get; } = new();
    }
}