using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// Unity 编辑器资产保存辅助类。
    /// </summary>
    public class EditorResHelper
    {
        /// <summary>
        /// 将指定的 Unity 资产标记为已修改（Dirty），保存到磁盘并刷新 <see cref="AssetDatabase"/>。
        /// </summary>
        /// <param name="asset">需要持久化保存的 Unity 资产对象。</param>
        public static void SaveAssets(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
