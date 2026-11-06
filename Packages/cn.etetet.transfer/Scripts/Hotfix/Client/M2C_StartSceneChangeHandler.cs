namespace ET.Client
{
	/// <summary>
	/// 客户端接收服务端通知开始切换场景（M2C_StartSceneChange）的消息处理器。
	/// </summary>
	[MessageHandler(SceneType.Client)]
	public class M2C_StartSceneChangeHandler : MessageHandler<Scene, M2C_StartSceneChange>
	{
		/// <summary>
		/// 触发客户端场景切换流程。
		/// </summary>
		protected override async ETTask Run(Scene root, M2C_StartSceneChange message)
		{
			await SceneChangeHelper.SceneChangeTo(root, message.SceneId, message.SceneName);
		}
	}
}
