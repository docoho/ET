namespace ET
{
    /// <summary>
    /// 技能配置 ID 规则判定辅助类（尾数为 0 表示主技能，非 0 表示由主技能派生的子技能）。
    /// </summary>
    public static class SpellConfigHelper
    {
        /// <summary>
        /// 判断指定技能配置 ID 是否为子技能（不检查公共与独立 CD）。
        /// </summary>
        public static bool IsSubSpell(int spellConfigId)
        {
            return spellConfigId % 10 != 0;
        }
        
        /// <summary>
        /// 判断指定技能配置 ID 是否为主技能（需检查 CD）。
        /// </summary>
        public static bool IsMainSpell(int spellConfigId)
        {
            return spellConfigId % 10 == 0;
        }
    }
}