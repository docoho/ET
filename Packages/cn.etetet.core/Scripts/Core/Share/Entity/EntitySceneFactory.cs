namespace ET
{
    /// <summary>
    /// 子场景创建工厂类。用于在已有的父实体（如 <c>CurrentScenesComponent</c> 或 <c>Root</c>）下创建并挂载子 <see cref="Scene"/>。
    /// </summary>
    public static class EntitySceneFactory
    {
        /// <summary>
        /// 在指定的 <paramref name="parent"/> 实体下创建一个新的子场景 <see cref="Scene"/> 并作为 Child 挂载。
        /// </summary>
        /// <param name="parent">父实体（不能为空，子场景将继承该父实体所在的 <see cref="Fiber"/>）</param>
        /// <param name="id">子场景的持久化业务 Id</param>
        /// <param name="sceneType">子场景类型（对应 <c>SceneType</c>）</param>
        /// <param name="name">子场景名称</param>
        public static Scene CreateScene(Entity parent, long id, int sceneType, string name)
        {
            Scene scene = new(parent.Fiber(), id, sceneType, name);
            parent?.AddChild(scene);
            return scene;
        }
    }
}