namespace ET.Client
{
    /// <summary>
    /// 客户端根场景（ClientScene）创建完成后的表现层事件回调：挂载资源加载器组件（ResourcesLoaderComponent）。
    /// </summary>
    [Event(SceneType.Client)]
    public class AfterCreateClientScene_AddComponent: AEvent<Scene, AfterCreateClientScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateClientScene args)
        {
            scene.AddComponent<ResourcesLoaderComponent>();
            await ETTask.CompletedTask;
        }
    }
}