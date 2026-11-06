using System;

namespace ET
{
    /// <summary>
    /// 技能编辑器常量与命名规范辅助类（约定主技能 ID 为 10 的倍数，资产命名为 s{Id} / b{Id}）。
    /// </summary>
    public static class SpellEditorConstants
    {
        public const string MenuPath = "ET/Spell/Spell Editor";
        public const string WindowTitle = "Spell Editor";
        public const string BtAssetRoot = "Packages/cn.etetet.statesync/Assets/BT";
        public const string SpellAssetPrefix = "s";
        public const string BuffAssetPrefix = "b";
        private const string LegacySpellAssetPrefix = "Spell";
        private const string LegacyBuffAssetPrefix = "Buff";
        public const int SpellGroupSize = 10;

        /// <summary>
        /// 判断给定 ID 是否为主技能 ID（约定为 10 的倍数）。
        /// </summary>
        public static bool IsMainSpell(int id)
        {
            return id % SpellGroupSize == 0;
        }

        /// <summary>
        /// 判断给定 ID 是否为子技能 ID（非 10 的倍数）。
        /// </summary>
        public static bool IsSubSpell(int id)
        {
            return id % SpellGroupSize != 0;
        }

        public static string SpellAssetName(int spellId)
        {
            return $"{SpellAssetPrefix}{spellId}";
        }

        public static string BuffAssetName(int buffId)
        {
            return $"{BuffAssetPrefix}{buffId}";
        }

        public static bool TryParseSpellAssetName(string assetName, out int spellId)
        {
            return TryParseAssetName(SpellAssetPrefix, LegacySpellAssetPrefix, assetName, out spellId);
        }

        public static bool TryParseBuffAssetName(string assetName, out int buffId)
        {
            return TryParseAssetName(BuffAssetPrefix, LegacyBuffAssetPrefix, assetName, out buffId);
        }

        private static bool TryParseAssetName(string prefix, string legacyPrefix, string assetName, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            string idText = assetName;
            if (assetName.StartsWith(prefix, StringComparison.Ordinal))
            {
                idText = assetName.Substring(prefix.Length);
            }
            else if (assetName.StartsWith(legacyPrefix, StringComparison.Ordinal))
            {
                idText = assetName.Substring(legacyPrefix.Length);
            }

            return TryParseId(idText, out id);
        }

        private static bool TryParseId(string idText, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(idText))
            {
                return false;
            }

            foreach (char c in idText)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return int.TryParse(idText, out id);
        }

        public static int MainSpellBase(int spellId)
        {
            return spellId / SpellGroupSize * SpellGroupSize;
        }
    }
}
