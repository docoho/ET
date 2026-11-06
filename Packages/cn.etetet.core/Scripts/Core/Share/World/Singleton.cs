using System;

namespace ET
{
    /// <summary>
    /// 所有单例的抽象非泛型基类，供 <see cref="World"/> 和 <see cref="Fiber"/> 容器统一存储与销毁。
    /// </summary>
    public abstract class ASingleton: Object, IDisposable
    {
        /// <summary>
        /// 当单例被加入容器时由框架内部调用（对于全局单例，会将自身赋值给 <see cref="Singleton{T}.Instance"/>）。
        /// </summary>
        internal abstract void Register();

        /// <summary>
        /// 控制在 <see cref="World.Dispose"/> 或 <see cref="Fiber.Dispose"/> 时的销毁优先级顺序。
        /// 数值越小越先被销毁；默认值 <c>10000</c>（例如 <c>FiberManager</c> 需要最先销毁，可重写返回更小的值）。
        /// </summary>
        public virtual int RemoveOrder()
        {
            return 10000;
        }

        public abstract void Dispose();
    }

    /// <summary>
    /// 泛型单例基类（奇异递归模板模式 CRTP：<c>class MySingleton : Singleton&lt;MySingleton&gt;</c>）。
    /// </summary>
    /// <remarks>
    /// 【初学者注意】
    /// - 只有标记了 <c>[AllowInstance]</c> 的进程级全局单例（注册在 <see cref="World"/> 上）才允许直接通过 <c>T.Instance</c> 静态属性访问；
    /// - 对于注册在 <see cref="Fiber"/> 上的纤程级单例，请通过 <c>fiber.GetSingleton&lt;T&gt;()</c> 或 <c>entity.GetSingleton&lt;T&gt;()</c> 获取，避免多线程静态字段覆盖。
    /// </remarks>
    public abstract class Singleton<T> : ASingleton where T : Singleton<T>
    {
        [StaticField]
        private static T instance;

        /// <summary>
        /// 进程级全局单例静态访问入口（仅当派生类标注了 <c>[AllowInstance]</c> 时允许业务代码直接访问）。
        /// </summary>
        [StaticField]
        public static T Instance
        {
            get
            {
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        internal override void Register()
        {
            Instance = (T)this;
        }

        /// <summary>
        /// 单例被销毁时调用的虚方法钩子，派生类可重写此方法清理内部字典、缓存或非托管资源。
        /// </summary>
        protected virtual void Destroy()
        {
        }

        public override void Dispose()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }

            this.Destroy();
        }
    }
}
