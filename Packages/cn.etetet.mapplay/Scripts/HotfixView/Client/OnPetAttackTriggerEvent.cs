namespace ET.Client
{
    /// <summary>
    /// 客户端触发宠物攻击事件处理器：读取主角当前选中的目标（TargetComponent.Unit），向服务端发送 C2M_PetAttack 指令。
    /// </summary>
    [Event(SceneType.Current)]
    public class OnPetAttackTriggerEvent : AEvent<Scene, OnPetAttackTrigger>
    {
        protected override async ETTask Run(Scene scene, OnPetAttackTrigger args)
        {
            Unit unit = args.Unit;
            if (unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null || targetComponent.Unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            C2M_PetAttack c2MPetAttack = C2M_PetAttack.Create();
            c2MPetAttack.UnitId = targetComponent.Unit.Id;
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MPetAttack);
            await ETTask.CompletedTask;
        }
    }
}
