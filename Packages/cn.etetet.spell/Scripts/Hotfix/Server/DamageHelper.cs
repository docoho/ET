using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端战斗伤害结算辅助类（负责生命值扣减、SpellMod 伤害/仇恨修正、仇恨累加、受击 Buff 行为树触发以及死亡事件发布）。
    /// </summary>
    public static class DamageHelper
    {
        /// <summary>
        /// 结算攻击者 <paramref name="attacker"/> 通过 <paramref name="damageBuff"/> 对目标 <paramref name="target"/> 造成的伤害。
        /// </summary>
        public static void Damage(Unit attacker, Unit target, Buff damageBuff, int value)
        {
            // 计算命中
            
            NumericComponent numericComponent = target.NumericComponent;
            long hp = numericComponent.Get(NumericType.HP);
            if (hp < value)
            {
                value = (int) hp;
            }
            long v = hp - value;
            
            // spell mod
            SpellComponent spellComponent = attacker.GetComponent<SpellComponent>();
            int spellConfigId = damageBuff.GetSpellConfigId();
            int damagePct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_DAMAGE);
            v = (int)(v * (100 + damagePct) / 100f);
            
            // 计算护甲
            
            // 计算抗性
            
            // 计算暴击
            
            numericComponent.Set(NumericType.HP, v);

            // 加上仇恨
            ThreatComponent threatComponent = target.GetComponent<ThreatComponent>();
            if (threatComponent != null)
            {
                int threat = value;
                // 计算仇恨Mod
                int threatPct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_THREAT);
                threat = (int)(threat * (100 + threatPct) / 100f);
                threatComponent.AddThreat(attacker, threat);
            }

            // 触发被攻击Effect
            using ListComponent<Buff> hittedBuffs = ListComponent<Buff>.Create();
            target.GetComponent<BuffComponent>().GetByEffectType<EffectServerBuffHitted>(hittedBuffs);

            foreach (Buff buff in hittedBuffs)
            {
                if (buff == null)
                {
                    Log.Error("hitted buff is null");
                    continue;
                }

                EffectServerBuffHitted effect = buff.GetConfig().GetEffect<EffectServerBuffHitted>();
                if (effect != null)
                {
                    using BTEnv env = BTEnv.Create(attacker.Scene(), target.Id);
                    env.AddEntity(effect.Attacker, attacker);
                    env.AddEntity(effect.Unit, target);
                    env.AddEntity(effect.Buff, buff);
                    BTDispatcher.Instance.Handle(effect, env);
                }
            }

            // 死亡
            if (v == 0)
            {
                EventSystem.Instance.Publish(target.Scene(), new UnitDie { Unit = attacker, Target = target});
            }
        }
    }
}