using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ET.Server
{
    /// <summary>
    /// 服务端 Buff 生命周期管理核心辅助类（负责 Buff 创建、叠加规则结算、Tick/超时定时器注册、行为树效果触发及客户端网络同步）。
    /// </summary>
    public static class BuffHelper
    {
        /// <summary>
        /// Buff 持续时间到期定时器回调。
        /// </summary>
        [Invoke(TimerInvokeType.BuffTimeoutTimer)]
        public class BuffTimeoutTimer: ATimer<Buff>
        {
            /// <summary>
            /// 到期时以 <see cref="BuffFlags.TimeoutRemove"/> 原因移除该 Buff。
            /// </summary>
            protected override void Run(Buff self)
            {
                BuffHelper.RemoveBuff(self, BuffFlags.TimeoutRemove);
            }
        }

        /// <summary>
        /// 创建 Buff 实体并建立父子 Buff 关联，但暂不执行初始化与叠加效果（常用于施法流程中先挂载 <see cref="SpellTargetComponent"/> 再统一初始化）。
        /// </summary>
        public static Buff CreateBuffWithoutInit(Unit unit, long casterId, long buffId, int buffConfigId, Buff parent)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffId, buffConfigId, casterId);
            BuffConfig buffConfig = buff.GetConfig();
            
            if (parent != null)
            {
                buff.GetBuffData().ParentData = parent.GetBuffData();
                
                if (buffConfig.Flags.Contains(BuffFlags.ParentRemoveRemove))
                {
                    BuffChildrenComponent buffChildrenComponent =
                            parent.GetComponent<BuffChildrenComponent>() ?? parent.AddComponent<BuffChildrenComponent>();
                    buffChildrenComponent.Buffs.Add(buff);
                }
            }
            
            Log.Debug($"Buff Create: {buffConfigId}");
            return buff;
        }

        /// <summary>
        /// 创建并立即初始化 Buff 实体（包含叠加规则判定、定时器注册、触发 <see cref="EffectServerBuffAdd"/> 行为树以及广播 <see cref="M2C_BuffAdd"/>）。
        /// </summary>
        public static Buff CreateBuff(Unit unit, long casterId, long buffId, int buffConfigId, Buff parent)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffId, buffConfigId, casterId);

            if (parent != null)
            {
                buff.GetBuffData().ParentData = parent.GetBuffData();
                BuffConfig buffConfig = buff.GetConfig();
                if (buffConfig.Flags.Contains(BuffFlags.ParentRemoveRemove))
                {
                    BuffChildrenComponent buffChildrenComponent =
                            parent.GetComponent<BuffChildrenComponent>() ?? parent.AddComponent<BuffChildrenComponent>();
                    buffChildrenComponent.Buffs.Add(buff);
                }
            }

            Log.Debug($"Buff Create: {buffConfigId}");
            return InitBuff(buff);
        }
        
        /// <summary>
        /// 初始化 Buff：处理同配置 ID 的叠加规则（叠层、延长时间、替换），挂载 Tick 与超时定时器，运行服务端添加效果行为树，并通知客户端。
        /// </summary>
        public static Buff InitBuff(Buff buff)
        {
            Unit unit = buff.Parent.GetParent<Unit>();
            BuffComponent buffComponent = buff.GetParent<BuffComponent>();
            BuffConfig buffConfig = buff.GetConfig();

            // 处理叠加规则
            if (buffConfig.OverLayRuleType != OverLayRuleType.None)
            {
                var oldBuffs = buffComponent.GetByConfigId(buffConfig.Id);
                if (oldBuffs != null && oldBuffs.Count > 0)
                {
                    switch (buffConfig.OverLayRuleType)
                    {
                        case OverLayRuleType.AddStack:
                        {
                            Buff oldBuff = oldBuffs.First();
                            UpdateStack(oldBuff, oldBuff.Stack + buffConfig.Stack);
                            return oldBuff;
                        }
                        case OverLayRuleType.AddTime:
                        {
                            Buff oldBuff = oldBuffs.First();
                            long expireTime = unit.GetSingleton<TimeInfo>().ServerNow() + buffConfig.Duration;
                            if (expireTime < oldBuff.ExpireTime)
                            {
                                return oldBuff;
                            }
                            UpdateExpireTime(oldBuff, expireTime);
                            return oldBuff;
                        }
                        case OverLayRuleType.Replace:
                        {
                            foreach (Buff oldBuff in oldBuffs.ToArray())
                            {
                                if (oldBuff.Id != buff.Id)
                                {
                                    RemoveBuff(oldBuff, BuffFlags.SameConfigIdReplaceRemove);
                                }
                            }
                            break;
                        }
                        case OverLayRuleType.None:
                        {
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            TimerComponent timerComponent = buff.Root().TimerComponent;

            RefreshTickComponent(buff);
            
            if (buff.ExpireTime > 0 && buff.ExpireTime < long.MaxValue)
            {
                if (buff.TickTime > 0)
                {
                    buff.ExpireTime += 1;
                }
                buff.TimeoutTimer = timerComponent.NewOnceTimer(buff.ExpireTime, TimerInvokeType.BuffTimeoutTimer, buff);
            }
            
            EffectServerBuffAdd effect = buff.GetConfig().GetEffect<EffectServerBuffAdd>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, buff.Parent.GetParent<Unit>());
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }
            
            
            M2C_BuffAdd m2CBuffAdd = M2C_BuffAdd.Create();
            m2CBuffAdd.BuffId = buff.Id;
            m2CBuffAdd.BuffConfigId = buffConfig.Id;
            m2CBuffAdd.UnitId = unit.Id;
            m2CBuffAdd.CreateTime = buff.CreateTime;
            m2CBuffAdd.TickTime = buff.TickTime;
            m2CBuffAdd.ExpireTime = buff.ExpireTime;
            m2CBuffAdd.Stack = buff.Stack;
            m2CBuffAdd.CasterId = buff.Caster;
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            if (spellTargetComponent != null)
            {
                m2CBuffAdd.SpellTarget = SpellTarget.Create();
                foreach (long targetId in spellTargetComponent.Units)
                {
                    m2CBuffAdd.SpellTarget.TargetUnitId.Add(targetId);
                }
                m2CBuffAdd.SpellTarget.TargetPosition = spellTargetComponent.Position;
            }
            
            MapMessageHelper.NoticeClient(unit, m2CBuffAdd, buffConfig.NoticeType);

            return buff;
        }

        /// <summary>
        /// 根据 <see cref="Buff.TickTime"/> 刷新或重建 <see cref="BuffTickComponent"/>。
        /// </summary>
        private static void RefreshTickComponent(Buff buff)
        {
            buff.RemoveComponent<BuffTickComponent>();

            if (buff.TickTime > 0)
            {
                buff.AddComponent<BuffTickComponent>();
            }
        }
        
        /// <summary>
        /// 更新 Buff 的过期时间戳，重新注册超时定时器并向客户端同步 <see cref="M2C_BuffUpdate"/>。
        /// </summary>
        public static void UpdateExpireTime(Buff buff, long expireTime)
        {
            buff.ExpireTime = expireTime;
            
            if (buff.ExpireTime < buff.GetSingleton<TimeInfo>().ServerNow())
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.TimeoutRemove);
                return;
            }
            
            
            long timeoutTimer = buff.TimeoutTimer;
            TimerComponent timerComponent = buff.Root().TimerComponent;
            timerComponent.Remove(ref timeoutTimer);
            
            if (buff.ExpireTime > 0)
            {
                buff.TimeoutTimer = timerComponent.NewOnceTimer(buff.ExpireTime, TimerInvokeType.BuffTimeoutTimer, buff);
            }

            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = -1;
            m2CBuffUpdate.ExpireTime = expireTime;
            m2CBuffUpdate.TickTime = -1;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buff.GetConfig().NoticeType);
        }
        
        /// <summary>
        /// 更新 Buff 的周期触发间隔时间（TickTime），刷新 <see cref="BuffTickComponent"/> 并通知客户端。
        /// </summary>
        public static void UpdateTickTime(Buff buff, int tickTime)
        {
            int oldTickTime = buff.TickTime;
            buff.TickTime = tickTime;

            if (oldTickTime <= 0 && tickTime > 0 && buff.ExpireTime > 0 && buff.ExpireTime < long.MaxValue)
            {
                UpdateExpireTime(buff, buff.ExpireTime + 1);
            }

            RefreshTickComponent(buff);
             
            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = -1;
            m2CBuffUpdate.ExpireTime = -1;
            m2CBuffUpdate.TickTime = tickTime;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buff.GetConfig().NoticeType);
        }
        
        /// <summary>
        /// 更新 Buff 的叠加层数（限制在 <c>[0, MaxStack]</c> 之间，层数归零时自动移除 Buff，否则通知客户端更新）。
        /// </summary>
        public static void UpdateStack(Buff buff, int stack)
        {
            if (buff.Stack == stack)
            {
                return;
            }

            BuffConfig buffConfig = buff.GetConfig();
            int maxStack = buffConfig.MaxStack;
            if (stack > maxStack)
            {
                stack = maxStack;
            }
            
            if (stack < 0)
            {
                stack = 0;
            }
            
            buff.Stack = stack;
            
            if (buff.Stack == 0)
            {
                RemoveBuff(buff, BuffFlags.StackRemove);
                return;
            }
            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = buff.Stack;
            m2CBuffUpdate.ExpireTime = -1;
            m2CBuffUpdate.TickTime = -1;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buffConfig.NoticeType);
        }

        /// <summary>
        /// 移除指定的 <paramref name="buff"/>，记录移除原因（<paramref name="removeType"/>），触发 <see cref="EffectServerBuffRemove"/> 行为树并通知客户端。
        /// </summary>
        public static void RemoveBuff(Buff buff, BuffFlags removeType)
        {
            Unit unit = buff.Parent?.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }
            
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            
            M2C_BuffRemove m2CBuffRemove = M2C_BuffRemove.Create();
            m2CBuffRemove.UnitId = unit.Id;
            m2CBuffRemove.BuffId = buff.Id;
            m2CBuffRemove.BuffConfigId = buff.ConfigId;
            m2CBuffRemove.RemoveType = (int)removeType;

            BuffRemoveTypeComponent buffRemoveTypeComponent = buff.GetComponent<BuffRemoveTypeComponent>() ?? buff.AddComponent<BuffRemoveTypeComponent>();
            buffRemoveTypeComponent.BuffRemoveType = removeType;
            
            BuffConfig buffConfig = buff.GetConfig();
            EffectServerBuffRemove effect = buffConfig.GetEffect<EffectServerBuffRemove>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            buffComponent.RemoveBuff(buff);
            
            MapMessageHelper.NoticeClient(unit, m2CBuffRemove, buffConfig.NoticeType);
        }
        
        /// <summary>
        /// 按 Buff 实体 ID 移除单位身上的指定 Buff。
        /// </summary>
        public static void RemoveBuff(Unit unit, long id, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            
            if (buff == null)
            {
                return;
            }
            
            RemoveBuff(buff, removeType);
        }

        /// <summary>
        /// 按 Buff 配置表 ID 批量移除单位身上所有匹配的 Buff。
        /// </summary>
        public static void RemoveBuffByConfigId(Unit unit, int configId, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByConfigId(configId);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, removeType);
            }
        }
        
        /// <summary>
        /// 移除单位身上所有带有指定 <paramref name="flag"/> 标签的 Buff（并以该 <paramref name="flag"/> 作为移除原因）。
        /// </summary>
        public static void RemoveBuffFlag(Unit unit, BuffFlags flag)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return;
            }
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByFlag(flag);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, flag);
            }
        }

        /// <summary>
        /// 移除单位身上所有带有指定 <paramref name="flag"/> 标签的 Buff，并显式指定移除原因 <paramref name="removeType"/>。
        /// </summary>
        public static void RemoveBuffsByFlag(Unit unit, BuffFlags flag, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return;
            }
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByFlag(flag);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, removeType);
            }
        }
    }
}
