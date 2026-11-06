namespace ET.Client
{
    /// <summary>
    /// 客户端场景切换进度更新事件。
    /// </summary>
    public struct ChangeSceneProgress
    {
        /// <summary>
        /// 当前场景加载进度（0~100）。
        /// </summary>
        public float Progress;
    }
    
    /// <summary>
    /// 客户端当前地图子场景管理组件（挂载在客户端根 Scene 上，管理当前的 <see cref="SceneType.Current"/> 子场景及切场景进度）。
    /// </summary>
    // 可以用来管理多个客户端场景，比如大世界会加载多块场景
    [ComponentOf(typeof(Scene))]
    public class CurrentScenesComponent: Entity, IAwake
    {
        private float progress;
        
        /// <summary>
        /// 获取或设置场景切换加载进度（设置时自动向根 Scene 发布 <see cref="ChangeSceneProgress"/> 事件）。
        /// </summary>
        public float Progress
        {
            get
            {
                return this.progress;
            }
            set
            {
                EventSystem.Instance.Publish(this.Root(), new ChangeSceneProgress() {Progress = value});
                this.progress = value;
            }
        }
        
        private EntityRef<Scene> scene;

        /// <summary>
        /// 获取或设置当前客户端所在的地图子场景（<see cref="SceneType.Current"/>）。
        /// </summary>
        public Scene Scene
        {
            get
            {
                return this.scene;
            }
            set
            {
                this.scene = value;
            }
        }
    }
}