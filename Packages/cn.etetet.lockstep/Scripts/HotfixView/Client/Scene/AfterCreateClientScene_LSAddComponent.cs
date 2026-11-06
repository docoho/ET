namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端子场景创建后事件（<see cref="AfterCreateClientScene"/>）处理器
    /// </summary>
    [Event(SceneType.LockStep)]
    public class AfterCreateClientScene_LSAddComponent: AEvent<Scene, AfterCreateClientScene>
    {
        /// <summary>
        /// 为新建的客户端场景挂载 <see cref="ResourcesLoaderComponent"/>
        /// </summary>
        protected override async ETTask Run(Scene scene, AfterCreateClientScene args)
        {
            scene.AddComponent<ResourcesLoaderComponent>();
            await ETTask.CompletedTask;
        }
    }
}