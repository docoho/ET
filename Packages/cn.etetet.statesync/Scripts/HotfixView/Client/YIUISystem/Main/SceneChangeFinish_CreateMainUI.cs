namespace ET.Client
{
    /// <summary>
    /// 场景切换完成事件（<see cref="SceneChangeFinish"/>）处理器，负责打开战斗主界面 <see cref="MainPanelComponent"/> 与头顶血条面板 <see cref="HUDPanelComponent"/>
    /// </summary>
    [Event(SceneType.Client)]
    public class SceneChangeFinish_CreateMainUI: AEvent<Scene, SceneChangeFinish>
    {
        /// <summary>
        /// 在当前子场景的 YIUI 根节点上异步打开 MainPanel 与 HUDPanel
        /// </summary>
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            Scene currentScene = scene.GetComponent<CurrentScenesComponent>().Scene;
            YIUIRootComponent yiuiRoot = currentScene.GetComponent<YIUIRootComponent>();
            EntityRef<YIUIRootComponent> yiuiRootRef = yiuiRoot;
            await yiuiRoot.OpenPanelAsync<MainPanelComponent>();
            yiuiRoot = yiuiRootRef;
            await yiuiRoot.OpenPanelAsync<HUDPanelComponent>();
        }
    }
}
