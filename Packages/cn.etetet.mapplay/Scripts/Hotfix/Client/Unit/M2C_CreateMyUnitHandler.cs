namespace ET.Client
{
	/// <summary>
	/// 客户端收到服务端创建主角通知（M2C_CreateMyUnit）的消息处理器：唤醒正在等待主角数据的场景切换协程。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_CreateMyUnitHandler: MessageHandler<Scene, M2C_CreateMyUnit>
	{
		protected override async ETTask Run(Scene currentScene, M2C_CreateMyUnit message)
		{
			// 通知场景切换协程继续往下走
			currentScene.Root().GetComponent<ObjectWait>().Notify(new Wait_CreateMyUnit() {Message = message});
			await ETTask.CompletedTask;
		}
	}
}
