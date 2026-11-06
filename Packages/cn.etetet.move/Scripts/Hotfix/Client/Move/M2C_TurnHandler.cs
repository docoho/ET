using Unity.Mathematics;

namespace ET.Client
{
	/// <summary>
	/// 客户端接收服务端广播的单位转向消息（M2C_Turn）的处理器。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_TurnHandler : MessageHandler<Scene, M2C_Turn>
	{
		/// <summary>
		/// 驱动目标单位的 <see cref="TurnComponent"/> 执行平滑转向。
		/// </summary>
		protected override async ETTask Run(Scene currentScene, M2C_Turn message)
		{
			Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
			if (unit == null)
			{
				return;
			}

			TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
			turnComponent.Turn(message.Rotation, message.TurnTime);
			await ETTask.CompletedTask;
		}
	}
}
