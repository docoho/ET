#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 编辑器下用于可视化配置单个 <see cref="ET.SpellConfig"/>（包含目标选择器与消耗行为树）的 Odin ScriptableObject 资产。
    /// </summary>
    [CreateAssetMenu(menuName = "ET/SpellScriptableObject")]
    [EnableClass]
    [HideMonoScript]
    public class SpellScriptableObject : SerializedScriptableObject
    {
        private const string SpellAssetPrefix = "s";
        private const string LegacySpellAssetPrefix = "Spell";

        /// <summary>
        /// 序列化保存的技能配置数据对象。
        /// </summary>
        [Title("技能 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public SpellConfig SpellConfig = new();

        /// <summary>
        /// 编辑器校验钩子：自动从资产文件名（如 <c>s100010</c> 或 <c>Spell100010</c>）解析并同步 <see cref="SpellConfig.Id"/>。
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!TryParseAssetName(SpellAssetPrefix, LegacySpellAssetPrefix, this.name, out int id))
            {
                return;
            }

            this.SpellConfig.Id = id;    // ScriptableObject 的 name 就是 asset 名称
#endif
        }

        /// <summary>
        /// 尝试剥离资产名称前缀并解析整数配置 ID。
        /// </summary>
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

        /// <summary>
        /// 校验字符串是否全为数字并解析为 int。
        /// </summary>
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
    }
}
#endif
