namespace ET
{
    /// <summary>
    /// <see cref="Buff"/> 实体系统，提供配置查询、父子 Buff 追溯、施法者/拥有者获取及过期判断。
    /// </summary>
    [EntitySystemOf(typeof(Buff))]
    public static partial class BuffSystem
    {
        /// <summary>
        /// Buff 实体销毁时自动移除其关联的超时定时器。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this Buff self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            TimerComponent timerComponent = root.TimerComponent;
            timerComponent.Remove(ref self.TimeoutTimer);
        }

        /// <summary>
        /// Buff 实体唤醒时记录配置表 ID。
        /// </summary>
        [EntitySystem]
        private static void Awake(this Buff self, int configId)
        {
            self.ConfigId = configId;
        }

        /// <summary>
        /// 通过 <see cref="BuffData.ParentData"/> 链路获取创建当前 Buff 的父级 Buff。
        /// </summary>
        public static Buff GetParentBuff(this Buff self)
        {
            BuffData parentData = self.GetBuffData().ParentData;
            if (parentData == null)
            {
                return null;
            }
            return parentData.GetParent<Buff>();
        }

        /// <summary>
        /// 获取或按需挂载用于跨生命周期传递目标等上下文数据的 <see cref="BuffData"/> 组件。
        /// </summary>
        public static BuffData GetBuffData(this Buff buff)
        {
            BuffData buffData = buff.BuffData;
            return buffData ?? buff.AddComponent<BuffData>();
        }

        /// <summary>
        /// 获取当前 Buff 对应的 <see cref="BuffConfig"/> 配置对象。
        /// </summary>
        public static BuffConfig GetConfig(this Buff self)
        {

            return self.Fiber().GetSingleton<BuffConfigCategory>().Get(self.ConfigId);
        }

        /// <summary>
        /// 获取当前 Buff 所属主技能的 <see cref="SpellConfig"/> 配置对象。
        /// </summary>
        public static SpellConfig GetSpellConfig(this Buff self)
        {
            return self.Fiber().GetSingleton<SpellConfigCategory>().Get(self.GetSpellConfigId());
        }

        /// <summary>
        /// 根据编号规则换算当前 Buff 归属的主技能配置 ID（100000~199999 为技能主/子 Buff，200000+ 为衍生 Buff）。
        /// </summary>
        public static int GetSpellConfigId(this Buff self)
        {
            if (self.ConfigId >= 100000 && self.ConfigId < 200000)
            {
                return self.ConfigId / 10 * 10;
            }

            int buffGroupId = self.ConfigId / 10 * 10;
            return buffGroupId - 100000;
        }

        /// <summary>
        /// 获取施加该 Buff 的施法者 <see cref="Unit"/>。
        /// </summary>
        public static Unit GetCaster(this Buff self)
        {
            return self.Scene().GetComponent<UnitComponent>().Get(self.Caster);
        }

        /// <summary>
        /// 获取当前挂载该 Buff 的宿主 <see cref="Unit"/>。
        /// </summary>
        public static Unit GetOwner(this Buff self)
        {
            return self.Parent.GetParent<Unit>();
        }

        /// <summary>
        /// 判断该 Buff 当前是否已到达或超过过期时间。
        /// </summary>
        public static bool IsExpired(this Buff self)
        {
            return self.ExpireTime <= self.GetSingleton<TimeInfo>().ServerNow() + 1;
        }
    }
}
