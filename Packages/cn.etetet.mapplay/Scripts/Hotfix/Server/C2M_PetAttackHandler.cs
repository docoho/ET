
namespace ET.Server
{
	/// <summary>
	/// 地图服处理客户端命令宠物攻击指定目标（C2M_PetAttack）的 ActorLocation 消息处理器。
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class C2M_PetAttackHandler : MessageLocationHandler<Unit, C2M_PetAttack>
	{
		protected override async ETTask Run(Unit unit, C2M_PetAttack message)
		{
			Unit target = unit.GetParent<UnitComponent>().Get(message.UnitId);
			if (target == null)
			{
				return;
			}

			Unit pet = PetHelper.GetPet(unit);
			if (pet == null)
			{
				return;
			}
			// 设置宠物的当前选中攻击目标，驱动宠物 AI 追击攻击
			pet.GetComponent<TargetComponent>().Unit = target;

			await ETTask.CompletedTask;
		}
	}
}