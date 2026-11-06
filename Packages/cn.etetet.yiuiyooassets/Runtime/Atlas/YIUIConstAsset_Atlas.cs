using System;
using Sirenix.OdinInspector;

namespace YIUIFramework
{
    /// <summary>
    /// YIUI 常量配置扩展（图集部分），定义全局图集索引数据资产的名称与保存路径。
    /// </summary>
    public partial class YIUIConstAsset
    {
        /// <summary>
        /// 生成的图集映射配置资产名称（YooAsset 寻址名）
        /// </summary>
        [BoxGroup("图集", CenterLabel = true)]
        [LabelText("生成图集资源名称")]
        [ReadOnly]
        public const string AtlasDataName = "YIUIAtlasData";

        /// <summary>
        /// 生成的图集映射配置资产在工程中的保存路径
        /// </summary>
        [BoxGroup("图集", CenterLabel = true)]
        [LabelText("生成图集资源路径")]
        [ReadOnly]
        public const string AtlasDataPath = "Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset";
    }
}