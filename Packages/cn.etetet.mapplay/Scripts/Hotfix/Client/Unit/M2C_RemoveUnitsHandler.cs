namespace ET.Client
{
	/// <summary>
	/// 客户端收到单位离开视野通知（M2C_RemoveUnits）的消息处理器：从当前场景 UnitComponent 移除对应单位。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_RemoveUnitsHandler: MessageHandler<Scene, M2C_RemoveUnits>
	{
		protected override async ETTask Run(Scene currentScene, M2C_RemoveUnits message)
		{	
			UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
			foreach (long unitId in message.Units)
			{
				unitComponent.Remove(unitId);
			}

			await ETTask.CompletedTask;
		}
	}
}
