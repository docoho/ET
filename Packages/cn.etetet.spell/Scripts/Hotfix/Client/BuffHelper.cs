namespace ET.Client
{
    /// <summary>
    /// 客户端 Buff 创建、更新与移除辅助类（驱动客户端侧 <see cref="EffectClientBuffAdd"/> 与 <see cref="EffectClientBuffRemove"/> 表现行为树）。
    /// </summary>
    public static class BuffHelper
    {
        /// <summary>
        /// 根据服务端下发的 <see cref="M2C_BuffAdd"/> 消息在客户端创建对应的 <see cref="Buff"/> 实体并执行客户端添加效果行为树。
        /// </summary>
        public static Buff CreateBuff(Unit unit, M2C_BuffAdd buffAdd)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffAdd.BuffId, buffAdd.BuffConfigId, buffAdd.CasterId);
            buff.ExpireTime = buffAdd.ExpireTime;
            buff.TickTime = buffAdd.TickTime;
            buff.CreateTime = buffAdd.CreateTime;
            buff.Stack = buffAdd.Stack;
            if (buffAdd.SpellTarget != null)
            {
                SpellTargetComponent spellTargetComponent = buff.GetBuffData().AddComponent<SpellTargetComponent>();
                spellTargetComponent.Units.AddRange(buffAdd.SpellTarget.TargetUnitId);
                spellTargetComponent.Position = buffAdd.SpellTarget.TargetPosition;
            }

            EffectClientBuffAdd effect = buff.GetConfig().GetEffect<EffectClientBuffAdd>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            Log.Debug($"Buff Create: {buffAdd.BuffConfigId}");
            return buff;
        }
        
        /// <summary>
        /// 更新客户端 Buff 的过期时间、Tick 间隔与层数。
        /// </summary>
        public static void UpdateBuff(Buff buff, long expireTime, int tickTime, int stack)
        {
            if (stack > 0)
            {
                buff.Stack = stack;
            }

            if (tickTime > 0)
            {
                buff.TickTime = tickTime;
            }

            if (expireTime > 0)
            {
                buff.ExpireTime = expireTime;
            }
        }
        
        /// <summary>
        /// 移除客户端单位身上的指定 Buff 并触发 <see cref="EffectClientBuffRemove"/> 客户端移除效果行为树。
        /// </summary>
        public static void RemoveBuff(Unit unit, long id, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            if (buff == null)
            {
                Log.Error($"not found buff: {id}");
                return;
            }
            buff.AddComponent<BuffRemoveTypeComponent>().BuffRemoveType = removeType;
            
            EffectClientBuffRemove effect = buff.GetConfig().GetEffect<EffectClientBuffRemove>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            buffComponent.RemoveBuff(buff);
        }
    }
}