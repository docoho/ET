namespace ET.Client
{
    /// <summary>
    /// 场景加载进度变更事件（<see cref="ChangeSceneProgress"/>）处理器，实时更新 Loading 界面进度条数值
    /// </summary>
    [Event(SceneType.Client)]
    public class ChangeSceneProgress_ShowProgress : AEvent<Scene, ChangeSceneProgress>
    {
        /// <summary>
        /// 将最新加载进度同步到 <see cref="LoadingPanelComponent"/> 的数据绑定字段
        /// </summary>
        protected override async ETTask Run(Scene scene, ChangeSceneProgress a)
        {
            scene.YIUIMgr().GetPanel<LoadingPanelComponent>()?.u_DataProgress.SetValue(a.Progress);
            await ETTask.CompletedTask;
        }
    }
}