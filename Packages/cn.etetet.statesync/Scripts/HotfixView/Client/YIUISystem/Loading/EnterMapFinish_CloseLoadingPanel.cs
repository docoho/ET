namespace ET.Client
{
    /// <summary>
    /// 进入地图完成事件（<see cref="EnterMapFinish"/>）处理器，延迟后关闭 Loading 界面
    /// </summary>
    [Event(SceneType.Client)]
    public class EnterMapFinish_CloseLoadingPanel : AEvent<Scene, EnterMapFinish>
    {
        /// <summary>
        /// 等待场景渲染稳定后关闭 <see cref="LoadingPanelComponent"/>
        /// </summary>
        protected override async ETTask Run(Scene scene, EnterMapFinish a)
        {
            EntityRef<Scene> sceneRef = scene;
            await scene.Root().TimerComponent.WaitAsync(2000);
            scene = sceneRef;
            scene.YIUIMgr().ClosePanel<LoadingPanelComponent>();
        }
    }
}