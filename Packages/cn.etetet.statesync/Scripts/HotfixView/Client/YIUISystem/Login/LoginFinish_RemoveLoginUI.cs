
namespace ET.Client
{
	/// <summary>
	/// 登录完成事件（<see cref="LoginFinish"/>）处理器，负责关闭登录界面 <see cref="LoginPanelComponent"/>
	/// </summary>
	[Event(SceneType.Client)]
	public class LoginFinish_RemoveLoginUI: AEvent<Scene, LoginFinish>
	{
		/// <summary>
		/// 异步关闭登录面板
		/// </summary>
		protected override async ETTask Run(Scene scene, LoginFinish args)
		{
			await scene.YIUIMgr().ClosePanelAsync<LoginPanelComponent>();
		}
	}
}
