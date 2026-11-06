using Unity.Mathematics;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 技能目标选择器根节点配置：从施法者位置朝向目标方向延伸的矩形区域目标选择器（支持客户端矩形技能指示器）。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorRectangle : TargetSelector
    {
        /// <summary>客户端显示的技能范围指示器预制体资源名称</summary>
#if UNITY
        [LabelText("技能指示器资源名")]
#endif
        public string SpellIndicatorName;

        /// <summary>矩形区域宽度</summary>
        public float Width;
        /// <summary>矩形区域长度</summary>
        public float Length;
        /// <summary>允许命中的目标单位类型掩码</summary>
        public UnitType UnitType;
    }
}
