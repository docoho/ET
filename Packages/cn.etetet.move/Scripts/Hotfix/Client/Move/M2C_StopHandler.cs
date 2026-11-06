using Unity.Mathematics;

namespace ET.Client
{
	/// <summary>
	/// 客户端接收服务端广播的停止移动消息（M2C_Stop）的处理器。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_StopHandler : MessageHandler<Scene, M2C_Stop>
	{
		/// <summary>
		/// 停止单位移动，校准服务端权威位置与朝向，并唤醒 <see cref="Wait_UnitStop"/> 等待协程。
		/// </summary>
		protected override async ETTask Run(Scene currentScene, M2C_Stop message)
		{
			Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.Id);
			if (unit == null)
			{
				return;
			}

			MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
			moveComponent.Stop(message.Error == 0);
			unit.Position = message.Position;
			unit.Rotation = message.Rotation;
			unit.GetComponent<ObjectWait>()?.Notify(new Wait_UnitStop() {Error = message.Error});
			await ETTask.CompletedTask;
		}
	}
}
