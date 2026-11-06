using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 进程级全局单例容器（World）。
    /// </summary>
    /// <remarks>
    /// 【初学者指南 —— <c>World</c> 与 <c>Fiber</c> 单例的区别】
    /// 1. <b>为什么不用传统的静态变量写单例？</b>
    ///    在游戏开发中（尤其是支持客户端热更新 Reload、或者退出登录返回标题界面重启时），传统散落在各个类里的静态变量很难按正确的依赖顺序统一清理和重建。
    ///    ET 将所有全局单例收拢到 <see cref="World.Instance"/> 容器中统一管理生命周期（通过 <see cref="ASingleton.RemoveOrder"/> 控制销毁顺序）。
    /// 2. <b><c>World.Instance.AddSingleton</c> vs <c>fiber.AddSingleton</c></b>：
    ///    - 挂载在 <see cref="World"/> 上的单例是<b>进程级（跨线程共享）</b>的，例如 <see cref="CodeTypes"/>、<see cref="EntitySystemSingleton"/>、<see cref="EventSystem"/>、<see cref="ObjectPool"/>、<see cref="IdGenerater"/>；
    ///    - 挂载在 <see cref="Fiber"/> 上的单例是<b>纤程级（单线程隔离）</b>的，不同 Fiber 各自拥有独立实例，互不干扰。
    /// </remarks>
    public class World : IDisposable
    {
        [StaticField]
        private static World instance;

        /// <summary>
        /// 获取进程唯一的 <see cref="World"/> 实例。
        /// </summary>
        [StaticField]
        public static World Instance
        {
            get
            {
                return instance ??= new World();
            }
        }

        /// <summary>按 <see cref="ASingleton.RemoveOrder"/> 排序的单例集合，保证 <see cref="Dispose"/> 时按从小到大的顺序安全销毁</summary>
        private readonly SortedDictionary<int, HashSet<ASingleton>> removeOrder = new();
        /// <summary>类型到单例实例的字典映射</summary>
        private readonly Dictionary<Type, ASingleton> singletons = new();

        private World()
        {
        }

        /// <summary>
        /// 销毁整个 World 容器，按照 <see cref="ASingleton.RemoveOrder"/> 顺序依次调用所有已注册全局单例的 <c>Dispose()</c>。
        /// </summary>
        public void Dispose()
        {
            instance = null;

            lock (this)
            {
                foreach (var kv in this.removeOrder)
                {
                    foreach (ASingleton singleton in kv.Value)
                    {
                        singleton.Dispose();
                    }
                }
                this.removeOrder.Clear();
                this.singletons.Clear();
            }
        }

        private void AddToOrder(ASingleton singleton)
        {
            int order = singleton.RemoveOrder();
            if (!this.removeOrder.TryGetValue(order, out HashSet<ASingleton> set))
            {
                set = new HashSet<ASingleton>();
                this.removeOrder[order] = set;
            }
            set.Add(singleton);
        }

        private void RemoveFromOrder(ASingleton singleton)
        {
            int order = singleton.RemoveOrder();
            if (this.removeOrder.TryGetValue(order, out HashSet<ASingleton> set))
            {
                set.Remove(singleton);
            }
        }

        /// <summary>
        /// 创建、初始化（调用无参 <c>Awake()</c>）并注册一个进程级全局单例 <typeparamref name="T"/>。
        /// </summary>
        public T AddSingleton<T>() where T : ASingleton, ISingletonAwake, new()
        {
            T singleton = new();
            singleton.Awake();

            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 创建、初始化（调用带 1 个参数的 <c>Awake(a)</c>）并注册一个进程级全局单例 <typeparamref name="T"/>。
        /// </summary>
        public T AddSingleton<T, A>(A a) where T : ASingleton, ISingletonAwake<A>, new()
        {
            T singleton = new();
            singleton.Awake(a);

            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 创建、初始化（调用带 2 个参数的 <c>Awake(a, b)</c>）并注册一个进程级全局单例 <typeparamref name="T"/>。
        /// </summary>
        public T AddSingleton<T, A, B>(A a, B b) where T : ASingleton, ISingletonAwake<A, B>, new()
        {
            T singleton = new();
            singleton.Awake(a, b);

            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 创建、初始化（调用带 3 个参数的 <c>Awake(a, b, c)</c>）并注册一个进程级全局单例 <typeparamref name="T"/>。
        /// </summary>
        public T AddSingleton<T, A, B, C>(A a, B b, C c) where T : ASingleton, ISingletonAwake<A, B, C>, new()
        {
            T singleton = new();
            singleton.Awake(a, b, c);

            AddSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// 将一个已实例化的 <see cref="ASingleton"/> 注册到 World 中，并触发其 <see cref="ASingleton.Register"/>（给 <c>Singleton&lt;T&gt;.Instance</c> 静态属性赋值）。
        /// </summary>
        public void AddSingleton(ASingleton singleton)
        {
            lock (this)
            {
                this.AddToOrder(singleton);
                this.singletons[singleton.GetType()] = singleton;
            }
            singleton.Register();
        }

        /// <summary>
        /// 尝试从 World 中获取指定类型的单例（线程安全）。
        /// </summary>
        public bool TryGetSingleton<T>(out T singleton) where T : class
        {
            lock (this)
            {
                if (this.singletons.TryGetValue(typeof(T), out ASingleton value))
                {
                    singleton = value as T;
                    return singleton != null;
                }
            }

            singleton = null;
            return false;
        }

        /// <summary>
        /// 从 World 中获取指定类型的单例（若未注册则返回 null）。
        /// </summary>
        public T GetSingleton<T>() where T : class
        {
            this.TryGetSingleton<T>(out T singleton);
            return singleton;
        }

        /// <summary>
        /// 热替换已存在的同类型单例（常用于热重载或测试替换），新单例注册后会自动销毁旧单例。
        /// </summary>
        public void ReplaceSingleton(ASingleton singleton)
        {
            ASingleton oldSingleton = null;

            lock (this)
            {
                Type type = singleton.GetType();
                if (this.singletons.TryGetValue(type, out oldSingleton))
                {
                    this.RemoveFromOrder(oldSingleton);
                }

                this.AddToOrder(singleton);
                this.singletons[type] = singleton;
            }

            singleton.Register();
            oldSingleton?.Dispose();
        }

        /// <summary>
        /// 从 World 中移除并销毁指定类型 <typeparamref name="T"/> 的单例。
        /// </summary>
        public void RemoveSingleton<T>()
        {
            this.RemoveSingleton(typeof(T));
        }

        /// <summary>
        /// 从 World 中移除并销毁指定 <paramref name="type"/> 的单例。
        /// </summary>
        public void RemoveSingleton(Type type)
        {
            ASingleton singleton = null;
            lock (this)
            {
                if (!this.singletons.Remove(type, out singleton))
                {
                    return;
                }
                this.RemoveFromOrder(singleton);
            }
            singleton?.Dispose();
        }
    }
}
