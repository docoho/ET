namespace ET.Client
{
    /// <summary>
    /// 客户端圆形区域施法目标选择器处理器（显示地面指示圈并等待玩家点击确认中心点坐标）。
    /// </summary>
    public class TargetSelectorCircleHandler : TargetSelectHandler<TargetSelectorCircle>
    {
        /// <summary>
        /// 异步等待玩家通过 <see cref="SpellIndicatorComponent"/> 选定地面坐标并写入 <see cref="TargetComponent.Position"/>。
        /// </summary>
        protected override async ETTask<int> Run(TargetSelectorCircle node, EntityRef<Unit> unitRef, SpellConfig spellConfig)
        {
            Unit unit = unitRef;
            SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            targetComponent.Position = await spellIndicatorComponent.WaitSpellIndicator(node);
            return 0;
        }
    }
}
