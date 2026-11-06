namespace ET.Client
{
    /// <summary>
    /// 客户端触发技能施放事件处理器：先调用客户端目标选择器（显示范围指示器或锁定目标），选择完成后调用 SpellHelper.Cast 向服务端请求施法。
    /// </summary>
    [Event(SceneType.Current)]
    public class OnSpellTriggerEvent: AEvent<Scene, OnSpellTrigger>
    {
        protected override async ETTask Run(Scene scene, OnSpellTrigger args)
        {
            int spellConfigId = args.SpellConfigId;
            SpellConfig spellConfig = scene.Fiber().GetSingleton<SpellConfigCategory>().Get(spellConfigId);
            
            C2M_SpellCast c2MSpellCast = C2M_SpellCast.Create();
            c2MSpellCast.SpellConfigId = spellConfigId;
            
            Unit unit = args.Unit;
            EntityRef<Unit> unitRef = unit;
            await TargetSelectDispatcher.Instance.Handle(spellConfig.TargetSelector, unitRef, spellConfig);

            unit = unitRef;
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            c2MSpellCast.TargetPosition = targetComponent.Position;
            SpellHelper.Cast(unit, c2MSpellCast);
        }
    }
}
