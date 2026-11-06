namespace ET.Client
{
    // 可以用来管理多个客户端场景，比如大世界会加载多块场景
    /// <summary>
    /// 客户端当前子场景引用管理组件（挂载在客户端根 <see cref="ET.Scene"/> 上）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class CurrentScenesComponent: Entity, IAwake
    {
        private EntityRef<Scene> scene;

        /// <summary>
        /// 当前激活的客户端子场景
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