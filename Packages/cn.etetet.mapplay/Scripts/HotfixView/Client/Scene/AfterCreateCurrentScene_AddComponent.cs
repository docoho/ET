namespace ET.Client
{
    /// <summary>
    /// 客户端当前地图子场景（CurrentScene）创建完成后的表现层事件回调：挂载场景级资源加载器（ResourcesLoaderComponent）与 YIUI 根组件（YIUIRootComponent）。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            scene.AddComponent<ResourcesLoaderComponent>();
            scene.AddComponent<YIUIRootComponent>();
            await ETTask.CompletedTask;
        }
    }
}