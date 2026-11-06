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
    /// 编辑器下用于可视化配置单个 <see cref="ET.BuffConfig"/> 及其行为树效果的 Odin ScriptableObject 资产。
    /// </summary>
    [CreateAssetMenu(menuName = "ET/BuffScriptableObject")]
    [EnableClass]
    [HideMonoScript]
    public class BuffScriptableObject : SerializedScriptableObject
    {
        private const string BuffAssetPrefix = "b";
        private const string LegacyBuffAssetPrefix = "Buff";

        /// <summary>
        /// 序列化保存的 Buff 配置数据对象。
        /// </summary>
        [Title("Buff 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public BuffConfig BuffConfig = new();

        /// <summary>
        /// 编辑器校验钩子：自动从资产文件名（如 <c>b100010</c> 或 <c>Buff100010</c>）解析并同步 <see cref="BuffConfig.Id"/>。
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!TryParseAssetName(BuffAssetPrefix, LegacyBuffAssetPrefix, this.name, out int id))
            {
                return;
            }

            this.BuffConfig.Id = id;    // ScriptableObject 的 name 就是 asset 名称
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
