namespace ET
{
    /// <summary>
    /// 技能目标选择器根节点配置：锁定单体目标选择器（检查当前选中目标合法性、距离及单位类型）。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorSingle : TargetSelector
    {
        /// <summary>输出黑板变量名：选中的单体目标单位（默认 "Unit"）</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        /// <summary>允许选中的目标单位类型掩码</summary>
        public UnitType UnitType;
    }
}