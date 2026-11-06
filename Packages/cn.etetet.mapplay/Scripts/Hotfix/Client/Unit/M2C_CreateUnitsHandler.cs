namespace ET.Client
{
	/// <summary>
	/// 客户端收到视野内新增单位列表（M2C_CreateUnits）的消息处理器：遍历创建尚不存在的客户端 Unit。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_CreateUnitsHandler: MessageHandler<Scene, M2C_CreateUnits>
	{
		protected override async ETTask Run(Scene currentScene, M2C_CreateUnits message)
		{
			UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
			
			foreach (UnitInfo unitInfo in message.Units)
			{
				if (unitComponent.Get(unitInfo.UnitId) != null)
				{
					continue;
				}
				Unit unit = UnitFactory.Create(currentScene, unitInfo);
			}
			await ETTask.CompletedTask;
		}
	}
}
