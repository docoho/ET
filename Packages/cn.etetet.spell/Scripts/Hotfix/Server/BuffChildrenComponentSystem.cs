namespace ET.Server
{
    /// <summary>
    /// 父 Buff 关联的子 Buff 集合组件系统（当父 Buff 移除销毁时，自动级联移除所有带有 <see cref="BuffFlags.ParentRemoveRemove"/> 标记的子 Buff）。
    /// </summary>
    [EntitySystemOf(typeof(BuffChildrenComponent))]
    public static partial class BuffChildrenComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffChildrenComponent self)
        {

        }
        
        /// <summary>
        /// 父 Buff 销毁时，级联移除其下属的所有子 Buff。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffChildrenComponent self)
        {
            foreach (Buff buff in self.Buffs)
            {
                if (buff == null)
                {
                    continue;
                }
                BuffHelper.RemoveBuff(buff, BuffFlags.ParentRemoveRemove);
            }
        }

    }
}