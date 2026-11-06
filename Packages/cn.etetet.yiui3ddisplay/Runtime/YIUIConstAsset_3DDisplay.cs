using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// YIUI全局常量配置资产的3D展示扩展部分。
    /// </summary>
    public partial class YIUIConstAsset
    {
        /// <summary>
        /// 3D模型在UI中渲染时专用的Unity Layer名称。
        /// </summary>
        [BoxGroup("3DDisplay", CenterLabel = true)]
        [LabelText("3D显示层名称")]
        public string YIUI3DLayer = "YIUI3DLayer";
    }
}