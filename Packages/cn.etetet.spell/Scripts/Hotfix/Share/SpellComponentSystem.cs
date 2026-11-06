using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 单位技能管理组件系统（负责公共 CD 与独立技能 CD 校验更新，以及技能修正值 SpellMod 的增减与读取）。
    /// </summary>
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this SpellComponent self)
        {

        }

        /// <summary>
        /// 检查指定技能是否已度过公共冷却时间（500ms）与自身独立冷却时间。
        /// </summary>
        public static bool CheckCD(this SpellComponent self, SpellConfig spellConfig)
        {
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();
            if (self.CDTime + 500 > timeNow)  // 公共CD
            {
                return false;
            }

            if (self.SpellCD.TryGetValue(spellConfig.Id, out long cdTime))
            {
                if (cdTime + spellConfig.CD > timeNow)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 记录指定技能最近一次施放的时间戳及公共 CD 时间戳。
        /// </summary>
        public static void UpdateCD(this SpellComponent self, int spellConfigId, long time)
        {
            self.CDTime = time;
            self.SpellCD[spellConfigId] = time;
        }

        /// <summary>
        /// 获取指定技能 ID 在特定修正类型（<see cref="SpellModType"/>）下的当前累计修正值。
        /// </summary>
        public static int GetMod(this SpellComponent self, int spellConfigId, SpellModType spellModType)
        {
            if (!self.SpellMods.TryGetValue(spellConfigId, out Dictionary<int, int> mods))
            {
                return 0;
            }
            return mods.GetValueOrDefault((int)spellModType, 0);
        }
        
        /// <summary>
        /// 为指定技能 ID 的特定修正类型累加或扣减修正值（归零时自动清理字典项）。
        /// </summary>
        public static void AddMod(this SpellComponent self, int spellConfigId, SpellModType spellModType, int value)
        {
            if (value == 0)
            {
                return;
            }
            
            if (!self.SpellMods.TryGetValue(spellConfigId, out Dictionary<int, int> mods))
            {
                mods = new Dictionary<int, int>();
                self.SpellMods[spellConfigId] = mods;
            }


            int modType = (int)spellModType;
            if (!mods.TryGetValue(modType, out int oldValue))
            {
                mods[modType] = value;
                return;
            }
            
            value = oldValue + value;
            if (value != 0)
            {
                mods[modType] = value;
                return;
            }

            mods.Remove(modType);
            if (mods.Count == 0)
            {
                self.SpellMods.Remove(spellConfigId);
            }
        }
    }
}