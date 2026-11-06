
namespace ET.Client
{
	/// <summary>
	/// 客户端启动初始化完成事件（<see cref="AppStartInitFinish"/>）处理器，负责初始化 YIUI 管理器并打开登录面板 <see cref="LoginPanelComponent"/>
	/// </summary>
	[Event(SceneType.Client)]
	public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
	{
		/// <summary>
		/// 注册 YIUI 事件单例，挂载资源加载、全局层级与 YIUI 管理器组件，初始化完成后打开登录面板
		/// </summary>
		protected override async ETTask Run(Scene root, AppStartInitFinish args)
		{
			EntityRef<Scene> rootRef = root;
			World.Instance.AddSingleton<YIUIEventComponent>();
			root.AddComponent<ResourcesLoaderComponent>();
			root.AddComponent<GlobalComponent>();
			bool result = await root.AddComponent<YIUIMgrComponent>().Initialize();
			if (!result)
			{
				Log.Error("初始化UI失败");
				return;
			}

			root = rootRef;
			await root.YIUIRoot().OpenPanelAsync<LoginPanelComponent>();
		}
	}
}
