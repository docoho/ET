
namespace ET.Client
{
	/// <summary>
	/// 登录完成事件（<see cref="LoginFinish"/>）处理器，负责打开游戏大厅界面 <see cref="LobbyPanelComponent"/>
	/// </summary>
	[Event(SceneType.Client)]
	public class LoginFinish_CreateLobbyUI: AEvent<Scene, LoginFinish>
	{
		/// <summary>
		/// 异步打开大厅面板
		/// </summary>
		protected override async ETTask Run(Scene scene, LoginFinish args)
		{
			await scene.YIUIRoot().OpenPanelAsync<LobbyPanelComponent>();
		}
	}
}
