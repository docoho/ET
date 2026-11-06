using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 进程级高性能通用对象池单例（线程安全）。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 1. <b>双模式设计</b>：
    ///    - 在 <b>Unity 编辑器模式（<c>UNITY_EDITOR</c>）</b>下，内部使用带锁的 <see cref="System.Collections.Generic.HashSet{T}"/> 存储池化对象，
    ///      一旦发现同一个对象被重复回收（Double Free），会立刻抛出异常 <c>"object already in pool"</c>，帮助开发者在研发期第一时间抓出重复入池 Bug！
    ///    - 在 <b>非编辑器/服务端生产环境</b>下，内部采用 <c>fastItem</c> 一级缓存槽 + <see cref="ConcurrentQueue{T}"/> 二级队列 + <see cref="Interlocked"/> 原子 CAS 操作，实现极速无锁并发存取（每个类型上限缓存 1000 个实例）。
    /// 2. 只有实现了 <see cref="IPool"/> 接口且 <c>IsFromPool == true</c> 的对象，在调用 <see cref="Recycle"/> 时才会真正调用 <c>Clear()</c> 并归还池中。
    /// </remarks>
    public class ObjectPool: Singleton<ObjectPool>, ISingletonAwake
    {
        private ConcurrentDictionary<Type, Pool> typePool;

        private readonly Func<Type, Pool> AddPoolFunc = _ => new Pool();

        public void Awake()
        {
            this.typePool = new ConcurrentDictionary<Type, Pool>();
        }

        /// <summary>
        /// 泛型方式从对象池获取一个 <typeparamref name="T"/> 实例（若 <paramref name="isFromPool"/> 为 false 或池为空则通过反射新建）。
        /// </summary>
        public static T Fetch<T>(bool isFromPool = true) where T : class
        {
            return Fetch(typeof (T), isFromPool) as T;
        }

        /// <summary>
        /// 按运行时 <see cref="Type"/> 从对象池获取对象实例。
        /// </summary>
        // 这里改成静态方法，主要为了兼容Unity Editor模式下没有初始化ObjectPool的情况
        public static object Fetch(Type type, bool isFromPool = true)
        {
            if (Instance == null)
            {
                return Activator.CreateInstance(type);
            }
            
            if (!isFromPool)
            {
                return Activator.CreateInstance(type);
            }
            
            Pool pool = Instance.GetPool(type);
            object obj = pool.Get(type);
            if (obj is IPool p)
            {
                p.IsFromPool = true;
            }
            return obj;
        }

        /// <summary>
        /// 回收对象到对象池，并自动将调用方的引用变量 <paramref name="obj"/> 置为 <c>null</c>（防止回收后误用）。
        /// </summary>
        public static void Recycle<T>(ref T obj) where T : class, IPool
        {
            Recycle(obj);
            obj = null;
        }

        /// <summary>
        /// 回收对象到对象池（会自动调用 <see cref="IPool.Clear"/> 清理状态）。
        /// </summary>
        public static void Recycle(object obj)
        {
            if (Instance == null)
            {
                return;
            }
            
            if (obj is not IPool p)
            {
                return;
            }
            
            if (!p.IsFromPool)
            {
                return;
            }
            
            p.Clear();
            
            // 这里注释掉，是为了早点发现重复入池的问题
            // 防止多次入池
            // p.IsFromPool = false;

            Type type = obj.GetType();
            Pool pool = Instance.GetPool(type);
            pool.Return(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pool GetPool(Type type)
        {
            return this.typePool.GetOrAdd(type, AddPoolFunc);
        }

        
#if UNITY_EDITOR

        /// <summary>
        /// 编辑器模式下会检测对象是否已经在池中，防止不小心多次入池,方便查找问题
        /// </summary>
        private class Pool
        {
            private const int MaxNum = 1000;

            private readonly System.Collections.Generic.HashSet<object> items = new();

            public object Get(Type type)
            {
                lock (this)
                {
                    if (this.items.Count <= 0)
                    {
                        return Activator.CreateInstance(type);
                    }

                    object obj = this.items.First();
                    this.items.Remove(obj);
                    return obj;
                }
            }

            public void Return(object obj)
            {
                lock (this)
                {
                    if (this.items.Count >= MaxNum)
                    {
                        return;
                    }
                    
                    if (!this.items.Add(obj))
                    {
                        throw new Exception("object already in pool: " + obj.GetType().FullName);
                    }
                }
            }
        }

#else

        /// <summary>
        /// 线程安全的无锁对象池（带 fastItem 一级热缓存槽，大幅减少高并发下的队列竞争）
        /// </summary>
        private class Pool
        {
            private const int MaxNum = 1000;
            
            private int numItems;
            private readonly ConcurrentQueue<object> items = new();
            private object fastItem;


            public object Get(Type type)
            {
                object item = this.fastItem;
                if (item == null || Interlocked.CompareExchange(ref this.fastItem, null, item) != item)
                {
                    if (this.items.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref this.numItems);
                        return item;
                    }

                    return Activator.CreateInstance(type);
                }

                return item;
            }

            public void Return(object obj)
            {
                if (this.fastItem != null || Interlocked.CompareExchange(ref this.fastItem, obj, null) != null)
                {
                    if (Interlocked.Increment(ref this.numItems) <= MaxNum)
                    {
                        this.items.Enqueue(obj);
                        return;
                    }

                    Interlocked.Decrement(ref this.numItems);
                }
            }
        }
#endif
    }
}
