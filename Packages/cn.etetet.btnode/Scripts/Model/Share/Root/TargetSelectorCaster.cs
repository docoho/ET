namespace ET
{
    /// <summary>
    /// 技能目标选择器根节点配置：直接以施法者自身（Caster）作为技能目标，无需额外朝向或范围指示器。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorCaster : TargetSelector
    {
    }
}