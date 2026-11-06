using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// YIUI 单个图集映射信息结构，记录图集名称与其所包含的所有 Sprite 名称列表。
    /// </summary>
    [Serializable]
    public class YIUIAtlasInfo
    {
        /// <summary>
        /// 图集资源名称（用于 YooAsset 寻址加载 SpriteAtlas）
        /// </summary>
        [LabelText("图集")]
        [ReadOnly]
        public string AtlasName;

        /// <summary>
        /// 该图集内包含的所有散图（Sprite）名称数组
        /// </summary>
        [LabelText("图片")]
        [ReadOnly]
        public string[] SpriteNames;
    }

    /// <summary>
    /// YIUI 全局图集映射数据配置资产（ScriptableObject），由编辑器烘焙生成，用于运行时快速反查 Sprite 所属图集。
    /// </summary>
    public class YIUIAtlasData : ScriptableObject
    {
        /// <summary>
        /// 所有图集的映射信息数组
        /// </summary>
        [ReadOnly]
        public YIUIAtlasInfo[] Infos;
    }
}