using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// 单位 Buff 容器组件系统（维护按 <see cref="BuffFlags"/>、<see cref="EffectNode"/> 类型以及配置 ID 的多维索引字典，提供高效增删改查）。
    /// </summary>
    [EntitySystemOf(typeof(BuffComponent))]
    public static partial class BuffComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffComponent self)
        {

        }
        
        /// <summary>
        /// 反序列化恢复时重建 <see cref="BuffComponent.flagBuffs"/> 索引。
        /// </summary>
        [EntitySystem]
        private static void Deserialize(this BuffComponent self)
        {
            foreach (Buff buff in self.Children.Values)
            {
                if (buff == null)
                {
                    continue;
                }
                foreach (BuffFlags flag in buff.GetConfig().Flags)
                {
                    self.flagBuffs.Add((int)flag, buff);
                }
            }
        }

        /// <summary>
        /// 创建 <see cref="Buff"/> 子实体，初始化层数、TickTime 与过期时间，并注册到 Flag、Effect 与 ConfigId 索引表中。
        /// </summary>
        public static Buff CreateBuff(this BuffComponent self, long buffId, int buffConfigId, long casterId)
        {
            try
            {
                Buff buff = self.AddChildWithId<Buff, int>(buffId, buffConfigId);
                buff.Caster = casterId;
                buff.CreateTime = self.GetSingleton<TimeInfo>().ServerNow();
                BuffConfig buffConfig = buff.GetConfig();
                buff.Stack = buffConfig.Stack;
                foreach (BuffFlags buffFlag in buffConfig.Flags)
                {
                    self.flagBuffs.Add((int)buffFlag, buff);
                }

                foreach (EffectNode effectNode in buffConfig.Effects)
                {
                    self.effectBuffs.Add(effectNode.GetType(), buff);
                }
            
                self.configIdBuffs.Add(buffConfigId, buff);
            
                if (buffConfig.TickTime > 0)
                {
                    buff.TickTime = buffConfig.TickTime;
                }
                if (buffConfig.Duration >= 0)
                {
                    buff.ExpireTime = self.GetSingleton<TimeInfo>().ServerNow() + buffConfig.Duration;
                }
                else
                {
                    buff.ExpireTime = long.MaxValue;
                }
                return buff;
            }
            catch (Exception e)
            {
                throw new Exception($"create buff fail: {buffId} {buffConfigId} {casterId}", e);
            }
        }

        /// <summary>
        /// 按配置表 ID 移除所有匹配的 <see cref="Buff"/> 子实体。
        /// </summary>
        public static void RemoveBuffByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.Remove(configId, out var buffs);

            if (buffs == null)
            {
                return;
            }
            
            foreach (Buff buff in buffs)
            {
                if (buff == null)
                {
                    continue;
                }
                self.RemoveBuff(buff);
            }
        }

        /// <summary>
        /// 从所有分类索引字典中注销并销毁指定的 <paramref name="buff"/> 子实体。
        /// </summary>
        public static void RemoveBuff(this BuffComponent self, Buff buff)
        {
            BuffConfig buffConfig = buff.GetConfig();
            foreach (BuffFlags flag in buffConfig.Flags)
            {
                self.flagBuffs.Remove((int)flag, buff);
            }
            
            foreach (EffectNode effectNode in buffConfig.Effects)
            {
                self.effectBuffs.Remove(effectNode.GetType(), buff);
            }

            self.configIdBuffs.Remove(buffConfig.Id, buff);
            
            Log.Debug($"remove buff: {buff.Id} {buff.ConfigId}");
            // BuffData靠GC回收，方便传递数据
            buff.RemoveComponent<BuffData>(false);
            self.RemoveChild(buff.Id);
        }
        
        /// <summary>
        /// 按实体 ID 移除指定的 <see cref="Buff"/> 子实体。
        /// </summary>
        public static void RemoveBuff(this BuffComponent self, long buffId)
        {
            Buff buff = self.GetChild<Buff>(buffId);
            self.RemoveBuff(buff);
        }

        /// <summary>
        /// 判断当前是否拥有指定配置 ID 的 Buff。
        /// </summary>
        public static bool HasBuff(this BuffComponent self, int configId)
        {
            var buffs = self.GetByConfigId(configId);
            if (buffs == null)
            {
                return false;
            }
            return buffs.Count > 0;
        }

        /// <summary>
        /// 获取带有指定 <see cref="BuffFlags"/> 标记的所有 Buff 集合。
        /// </summary>
        public static HashSet<EntityRef<Buff>> GetByFlag(this BuffComponent self, BuffFlags flag)
        {
            self.flagBuffs.TryGetValue((int)flag, out var set);
            return set;
        }
        
        /// <summary>
        /// 获取指定配置表 ID 的所有 Buff 集合。
        /// </summary>
        public static HashSet<EntityRef<Buff>> GetByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.TryGetValue(configId, out var set);
            return set;
        }
        
        /// <summary>
        /// 获取指定配置表 ID 的第一个 Buff 实例。
        /// </summary>
        public static Buff GetOneByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.TryGetValue(configId, out var set);
            return set?.FirstOrDefault();
        }
        
        /// <summary>
        /// 直接从组件中移除所有带有指定 <see cref="BuffFlags"/> 标记的 Buff。
        /// </summary>
        public static void RemoveBuffFlag(this BuffComponent self, BuffFlags flag)
        {
            if (!self.flagBuffs.Remove((int)flag, out var set))
            {
                return;
            }
            foreach (Buff buff in set)
            {
                self.RemoveBuff(buff);
            }
        }
        
        /// <summary>
        /// 获取包含指定类型 <typeparamref name="T"/> 效果节点（如受击触发 <see cref="EffectServerBuffHitted"/>）的所有 Buff 列表。
        /// </summary>
        public static void GetByEffectType<T>(this BuffComponent self, List<Buff> buffs) where T: EffectNode
        {
            if (!self.effectBuffs.TryGetValue(typeof(T), out var set))
            {
                return;
            }
            foreach (var buffRef in set)
            {
                buffs.Add(buffRef);
            }
        }
    }
}