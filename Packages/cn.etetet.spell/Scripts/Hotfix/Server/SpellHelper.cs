using System;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端技能施放核心辅助类。
    /// <remarks>
    /// ET10 的技能（Spell）在底层设计为“技能即 Buff”：
    /// 施放一个技能本质上是校验 CD -> 创建与该技能同配置 ID 的主 Buff -> 执行 <see cref="TargetSelector"/> 选敌行为树 -> 执行 <see cref="CostNode"/> 消耗检查与扣除 -> 更新 CD -> 初始化该技能 Buff。
    /// </remarks>
    /// </summary>
    public static class SpellHelper
    {
        /// <summary>
        /// 单位施放指定配置 ID 的技能（通过 <see cref="TargetSelector"/> 行为树选敌）。
        /// </summary>
        public static int Cast(Unit unit, int spellConfigId, Buff parent = null)
        {
            SpellConfig spellConfig = unit.Fiber().GetSingleton<SpellConfigCategory>().Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
#region Check
            // check
            {
                // 通用流程，子技能不检查CD
                if (SpellConfigHelper.IsMainSpell(spellConfigId))
                {
                    bool ret = spellComponent.CheckCD(spellConfig);
                    if (!ret)
                    {
                        return TextConstDefine.SpellCast_SpellInCD;
                    }
                }
            }
#endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.Id, parent);
            int castRet = SpellCasting(unit, buff, spellConfig, parent);
            if (castRet != 0)
            {
                unit.GetComponent<BuffComponent>().RemoveBuff(buff);
            }

            return castRet;
        }
        
        /// <summary>
        /// 指定目标Unit施放技能
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="spellConfigId"></param>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static int Cast(Unit unit, int spellConfigId, Unit target, Buff parent = null)
        {
            SpellConfig spellConfig = unit.Fiber().GetSingleton<SpellConfigCategory>().Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            #region Check
            // check
            {
                if (SpellConfigHelper.IsMainSpell(spellConfigId))
                {
                    bool ret = spellComponent.CheckCD(spellConfig);
                    if (!ret)
                    {
                        return TextConstDefine.SpellCast_SpellInCD;
                    }
                }
            }
            #endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.Id, parent);
            buff.GetBuffData().AddComponent<SpellTargetComponent>().Units.Add(target.Id);
            int castRet = SpellCasting(unit, buff, spellConfig, parent);
            if (castRet != 0)
            {
                unit.GetComponent<BuffComponent>().RemoveBuff(buff);
            }

            return castRet;
        }

        /// <summary>
        /// 技能施放内部流水线：打断旧主技能 -> 执行 <see cref="TargetSelector"/> 目标选择器 -> 执行 <see cref="CostNode"/> 消耗检查与扣除 -> 更新 CD -> 初始化技能 Buff。
        /// </summary>
        private static int SpellCasting(Unit unit, Buff buff, SpellConfig spellConfig, Buff parent)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            buff.Caster = unit.Id;

            if (parent == null)
            {
                // 这里简单做一下打断当前技能, 设置新的当前技能
                Buff currentBuff = spellComponent.Current;
                if (currentBuff != null)
                {
                    BuffHelper.RemoveBuff(currentBuff, BuffFlags.NewSpellInterrupt);
                }

                spellComponent.Current = buff;
            }

            // 选择目标
            {
                SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
                // 不为null表示已经指定了目标
                if (spellTargetComponent == null)
                {
                    buff.GetBuffData().AddComponent<SpellTargetComponent>();
                    TargetSelector targetSelector = spellConfig.TargetSelector;
                    if (targetSelector != null)
                    {
                        using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                        env.AddEntity(targetSelector.Buff, buff);
                        env.AddEntity(targetSelector.Caster, buff.GetCaster());
                        env.AddEntity(targetSelector.Owner, buff.GetOwner());
                        int ret = BTHelper.RunTree(spellConfig.TargetSelector, env);
                        if (ret != 0)
                        {
                            return ret;
                        }
                    }
                }
            }

            CostNode costNode = spellConfig.Cost;
            if (costNode != null)
            {
                // 先检查消耗的东西是否足够
                {
                    using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Unit, unit);
                    env.AddStruct(costNode.Check, true);
                    int ret = BTHelper.RunTree(costNode, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }

                // 消耗东西
                {
                    using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Unit, unit);
                    env.AddStruct(costNode.Check, false);
                    int ret = BTHelper.RunTree(costNode, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
            }
            
            // 这里不管是否检查，都会更新cd
            if (spellConfig.CD > 0)
            {
                UpdateCD(unit, spellConfig.Id, unit.GetSingleton<TimeInfo>().ServerNow());
            }
            
            BuffHelper.InitBuff(buff);
            
            if (buff.ExpireTime < unit.GetSingleton<TimeInfo>().ServerNow())
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
            }
            
            return 0;
        }

        /// <summary>
        /// 停止并移除单位当前正在施放的主技能 Buff。
        /// </summary>
        public static void Stop(Unit unit, BuffFlags buffFlags)
        {
            Buff buff = unit.GetComponent<SpellComponent>().Current;
            if (buff == null)
            {
                return;
            }
            BuffHelper.RemoveBuff(buff, buffFlags);
        }

        /// <summary>
        /// 更新单位指定技能的冷却时间戳，若是玩家单位则同步下发 <see cref="M2C_UpdateCD"/> 消息给客户端。
        /// </summary>
        public static void UpdateCD(Unit unit, int spellConfigId, long time)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.UpdateCD(spellConfigId, time);

            if (unit.UnitType == UnitType.Player)
            {
                M2C_UpdateCD m2CUpdateCd = M2C_UpdateCD.Create();
                m2CUpdateCd.UnitId = unit.Id;
                m2CUpdateCd.SpellConfigId = spellConfigId;
                m2CUpdateCd.Time = time;
                MapMessageHelper.NoticeClient(unit, m2CUpdateCd, NoticeType.Self);
            }
        }
    }
}
