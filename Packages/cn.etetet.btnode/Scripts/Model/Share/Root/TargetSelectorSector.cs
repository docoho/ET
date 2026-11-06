using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 技能目标选择器根节点配置：以施法者为顶点、朝向目标方向的扇形区域目标选择器（支持客户端扇形技能指示器）。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorSector : TargetSelector
    {
        /// <summary>客户端显示的技能范围指示器预制体资源名称</summary>
#if UNITY
        [LabelText("技能指示器资源名")]
#endif
        public string SpellIndicatorName;
        
        /// <summary>扇形半径</summary>
        public int Radius;
        /// <summary>扇形张角（角度制）</summary>
        public int Angle;
        /// <summary>允许命中的目标单位类型掩码</summary>
        public UnitType UnitType;
    }
}
