namespace ET
{
    /// <summary>
    /// <see cref="Entity"/> 常用上下文查询扩展方法集合。
    /// 在任何 System 方法中，都可以通过 <c>self.Root()</c>、<c>self.Scene()</c>、<c>self.Fiber()</c> 快速向上回溯所在场景与纤程。
    /// </summary>
    public static class EntityHelper
    {
        /// <summary>
        /// 获取当前实体所在纤程的区服编号（Zone ID，如 1 区、2 区）。
        /// </summary>
        public static int Zone(this Entity entity)
        {
            return entity.IScene.Fiber.Zone;
        }

        /// <summary>
        /// 获取当前实体直接归属的 <see cref="ET.Scene"/> 实例（例如客户端的 CurrentScene 或服务端的 MapScene）。
        /// </summary>
        public static Scene Scene(this Entity entity)
        {
            return entity.IScene as Scene;
        }
        
        /// <summary>
        /// 获取当前实体直接归属的强类型 <typeparamref name="T"/> 场景（例如帧同步中的 <c>self.Scene&lt;LSWorld&gt;()</c>）。
        /// </summary>
        public static T Scene<T>(this Entity entity) where T: class, IScene 
        {
            return entity.IScene as T;
        }
        
        /// <summary>
        /// 获取当前实体所在 <see cref="ET.Fiber"/> 的最顶层根场景（<see cref="Fiber.Root"/>）。
        /// 【初学者提示】许多纤程全局级别的组件（如 <c>TimerComponent</c>、<c>CoroutineLockComponent</c>、<c>NetComponent</c>）都挂载在 <c>self.Root()</c> 上！
        /// </summary>
        public static Scene Root(this Entity entity)
        {
            return entity.IScene.Fiber.Root;
        }
        
        /// <summary>
        /// 获取当前实体所属的 <see cref="ET.Fiber"/> 纤程实例。
        /// </summary>
        public static Fiber Fiber(this Entity entity)
        {
            return entity.IScene.Fiber;
        }

        /// <summary>
        /// 从当前实体所属的 <see cref="ET.Fiber"/> 中获取纤程级单例 <typeparamref name="T"/>。
        /// </summary>
        public static T GetSingleton<T>(this Entity entity) where T : Singleton<T>
        {
            return entity.Fiber().GetSingleton<T>();
        }
    }
}