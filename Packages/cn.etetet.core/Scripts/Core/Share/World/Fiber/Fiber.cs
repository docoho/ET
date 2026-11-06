using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    /// <summary>
    /// 纤程销毁前发布的同步清理事件结构体（各模块可监听此事件在 Fiber 销毁前做收尾工作）。
    /// </summary>
    public struct FiberDestroyEvent
    {
    }
    
    /// <summary>
    /// ET10 纤程（Fiber）：独立的单线程逻辑执行单元与 ECS 实体树宿主。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— 什么是 Fiber（纤程）？为什么要引入 Fiber？】
    /// 1. <b>把每个 Fiber 看作一台独立的“虚拟微服务器”</b>：
    ///    - 每个 <see cref="Fiber"/> 拥有自己独立的根场景（<see cref="Root"/>）、独立的实体更新系统（<see cref="EntitySystem"/>）、独立的 Actor 邮箱表（<see cref="Mailboxes"/>）以及独立的单线程同步上下文（<see cref="ThreadSynchronizationContext"/>）。
    ///    - 在同一个 <see cref="Fiber"/> 内部，所有的 <c>Update</c>、<c>LateUpdate</c>、<c>await</c> 回调和网络消息处理<b>永远按顺序在同一个线程上下文中串行执行</b>，因此业务代码在 Fiber 内部读写任何 Entity 数据都<b>完全不需要加多线程锁（lock）</b>！
    /// 2. <b>多核并发扩展</b>：
    ///    一个物理进程可以同时创建几十上百个 <see cref="Fiber"/>（例如每个大世界地图、每个副本房间、或者网关服务各自是一个 Fiber），
    ///    由 <see cref="FiberManager"/> 将它们分配到主线程（<see cref="SchedulerType.Main"/>）、独占线程（<see cref="SchedulerType.Thread"/>）或线程池（<see cref="SchedulerType.ThreadPool"/>）上并行驱动，轻松吃满服务器多核 CPU！
    /// 3. <b>跨 Fiber 通信铁律</b>：
    ///    不同 Fiber 之间绝对不允许直接读写对方的 <see cref="Entity"/> 引用（Debug 下 <c>CheckThread()</c> 会直接报错），必须通过 Actor 消息或 <c>ProcessInnerSender</c> 通信！
    /// </remarks>
    public class Fiber: IScheduler
    {
        /// <summary>
        /// 线程静态变量：记录当前物理线程正在调度执行的 <see cref="Fiber"/> 实例。
        /// </summary>
        // 该字段只能框架使用，绝对不能改成public，改了后果自负
        [StaticField]
        [ThreadStatic]
        private static Fiber instance;

        /// <summary>
        /// 框架内部专用：获取或切换当前线程绑定的 <see cref="Fiber"/> 上下文（会自动同步切换 <see cref="SynchronizationContext.Current"/>）。
        /// 业务逻辑严禁访问此属性，业务层请一律通过 <c>self.Fiber()</c> 或 <c>self.Root()</c> 获取！
        /// </summary>
        // 绝对禁止逻辑使用！！！
        [StaticField]
        internal static Fiber Instance
        {
            get
            {
                return instance;
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                instance = value;
                instance.Log.SceneName = instance.Name;
                SynchronizationContext.SetSynchronizationContext(instance.ThreadSynchronizationContext);
            }
        }
        
        /// <summary>标识当前纤程是否已销毁</summary>
        public bool IsDisposed { get; private set; }
        
        /// <summary>当前纤程的 64 位唯一 ID（高 32 位为 Zone 区服号，低 32 位为 LocalSlot 槽位号）</summary>
        public long Id { get; }

        /// <summary>从纤程 <see cref="Id"/> 中解码出的区服编号（Zone）</summary>
        public int Zone => FiberIdHelper.DecodeZone(this.Id);

        /// <summary>从纤程 <see cref="Id"/> 中解码出的进程内局部槽位号（LocalSlot）</summary>
        public int LocalSlot => FiberIdHelper.DecodeLocalSlot(this.Id);

        private EntityRef<Scene> root;
        
        /// <summary>当前纤程使用的线程调度策略类型</summary>
        public SchedulerType SchedulerType { get; }
        
        /// <summary>创建当前纤程的父纤程 ID（若为顶层主纤程则为 0）</summary>
        public long ParentFiberId { get; }

        private FiberMonitorInfo fiberMonitorInfo;

        /// <summary>纤程内部单调递增的实体 InstanceId 分配计数器（从 1 开始，Root Scene 的 InstanceId 固定为 1）</summary>
        private long instanceIdGenerator = 1;
        
        /// <summary>
        /// 为挂载到本纤程下的 <see cref="Entity"/> 分配一个新的唯一运行时 <see cref="Entity.InstanceId"/>。
        /// </summary>
        public long NewInstanceId()
        {
            return instanceIdGenerator++;
        }

        /// <summary>
        /// 当前纤程的根场景实体（<c>InstanceId == 1</c>）。纤程内所有的全局组件（如 <c>TimerComponent</c>、<c>CoroutineLockComponent</c>）均挂载在此节点上。
        /// </summary>
        public Scene Root
        {
            get
            {
                return this.root;
            }
            private set
            {
                this.root = value;
            }
        }

        /// <summary>纤程名称（同步作为根 Scene 名称与日志前缀）</summary>
        public string Name { get; }

        /// <summary>本纤程专属的 ECS 周期事件（Update / LateUpdate）轮询管理器</summary>
        public EntitySystem EntitySystem { get; }
        /// <summary>本纤程专属的 Actor 消息邮箱注册表（按 <c>InstanceId</c> 索引挂载了 <c>MailBoxComponent</c> 的实体）</summary>
        public Mailboxes Mailboxes { get; private set; }
        /// <summary>本纤程专属的单线程异步回调同步上下文（保证 <c>await</c> 延续回调回到本纤程单线程执行）</summary>
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; }
        /// <summary>本纤程专属的日志记录器实例</summary>
        public ILog Log { get; }

        /// <summary>等待当前帧结束（<see cref="WaitFrameFinish"/>）的任务队列</summary>
        private readonly Queue<ETTask> frameFinishTasks = new();
        
        /// <summary>以 <see cref="SchedulerType.Parent"/> 模式挂载在当前纤程下随父纤程一起调度的子纤程队列</summary>
        private readonly Queue<Fiber> schedulerQueue = new();

        /// <summary>当前纤程创建的所有子纤程字典</summary>
        private readonly Dictionary<long, Fiber> children = new();
        
        /// <summary>当前纤程局部的单例字典（支持多 Fiber 隔离的单例）</summary>
        private Dictionary<Type, object> singletons;
        
        internal Fiber(long id, long rootId, int sceneType, string name, SchedulerType schedulerType, Fiber parent)
        {
            this.Id = id;
            this.SchedulerType = schedulerType;
            this.EntitySystem = new EntitySystem();
            this.Mailboxes = new Mailboxes();
            this.ParentFiberId = parent?.Id ?? 0;
            this.Name = name;

            if (schedulerType == SchedulerType.Parent)
            {
                this.Log = parent.Log;
                // 注意调度是Parent的纤程，同步上下文必须是父亲的同步上下文
                // 因为父亲有可能会调用其方法，如果同步上下文不一样则父亲调用的上下文会发生改变
                // 比如Test_CreateRobot_Test中，父亲会调用创建出来的子Fiber的发送消息方法，那么子fiber等call回来之后，当前上下文变成了子fiber的了
                // 这样导致父亲在调用前的上下文跟调用后的上下文发生了改变
                this.ThreadSynchronizationContext = parent.ThreadSynchronizationContext;
            }
            else
            {
                this.ThreadSynchronizationContext = new ThreadSynchronizationContext();
                
                LogInvoker logInvoker = new() { SceneName = name };
                this.Log = EventSystem.Instance.Invoke<LogInvoker, ILog>(logInvoker);
            }
            this.Root = new Scene(this, rootId, sceneType, name);
        }
        
        /// <summary>
        /// 驱动当前纤程及其 <see cref="SchedulerType.Parent"/> 子纤程的每帧 <c>Update</c> 逻辑。
        /// </summary>
        internal void Update()
        {            
            try
            {                
                Instance = this;

                this.fiberMonitorInfo = new FiberMonitorInfo(this.Name);
                
                int t1 = Environment.TickCount;
                
                this.EntitySystem.Publish(new UpdateEvent());
                
                int t2 = Environment.TickCount;
                this.fiberMonitorInfo.UpdateTimeUsed = t2 - t1;

                int count = this.schedulerQueue.Count;
                while (count-- > 0)
                {
                    Fiber fiber = this.schedulerQueue.Dequeue();
                    
                    if (fiber.IsDisposed)
                    {
                        this.children.Remove(fiber.Id);
                        continue;
                    }
                    
                    this.schedulerQueue.Enqueue(fiber);
                    
                    fiber.Update();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        /// <summary>
        /// 驱动当前纤程及其 <see cref="SchedulerType.Parent"/> 子纤程的帧末 <c>LateUpdate</c>、帧结束等待回调以及 <see cref="ThreadSynchronizationContext"/> 异步队列回调。
        /// </summary>
        internal void LateUpdate()
        {
            try
            {
                Instance = this;

                int t1 = Environment.TickCount;
                
                this.EntitySystem.Publish(new LateUpdateEvent());
                FrameFinishUpdate();
                this.ThreadSynchronizationContext.Update();
                
                int t2 = Environment.TickCount;
                this.fiberMonitorInfo.LateUpdateTimeUsed = t2 - t1;

                FiberManager.Instance?.AddMonitor(this.Id, ref this.fiberMonitorInfo);

                int count = this.schedulerQueue.Count;
                while (count-- > 0)
                {
                    Fiber fiber = this.schedulerQueue.Dequeue();
                    
                    if (fiber.IsDisposed)
                    {
                        this.children.Remove(fiber.Id);
                        continue;
                    }
                    
                    this.schedulerQueue.Enqueue(fiber);
                    
                    fiber.LateUpdate();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// 异步等待直到当前帧的 <c>LateUpdate</c> 结束时唤醒恢复。
        /// </summary>
        public async ETTask WaitFrameFinish()
        {
            ETTask task = ETTask.Create(true);
            this.frameFinishTasks.Enqueue(task);
            await task;
        }

        private void FrameFinishUpdate()
        {
            while (this.frameFinishTasks.Count > 0)
            {
                ETTask task = this.frameFinishTasks.Dequeue();
                task.SetResult();
            }
        }

        /// <summary>
        /// 在当前纤程下创建一个共享父线程调度的子纤程（<see cref="SchedulerType.Parent"/>，继承当前纤程的 Zone）。
        /// </summary>
        public async ETTask<Fiber> CreateFiber(long rootId, int sceneType, string name)
        {
            Fiber fiber = await FiberManager.Instance.CreateFiber(SchedulerType.Parent, this.Zone, rootId, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber;
        }

        /// <summary>
        /// 在当前纤程下创建一个指定 <paramref name="zone"/> 且共享父线程调度的子纤程（<see cref="SchedulerType.Parent"/>）。
        /// </summary>
        public async ETTask<Fiber> CreateFiber(int zone, long rootId, int sceneType, string name)
        {
            Fiber fiber = await FiberManager.Instance.CreateFiber(SchedulerType.Parent, zone, rootId, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber;
        }

        /// <summary>
        /// 在当前纤程下创建一个指定调度策略（<paramref name="schedulerType"/>）的子纤程，并返回新子纤程的 <c>FiberId</c>。
        /// （注意：若非 <c>SchedulerType.Parent</c>，外部只能拿到它的 <c>long fiberId</c> 通过消息与它通信，而不能直接拿 <see cref="Fiber"/> 对象引用以防跨线程直接操作！）
        /// </summary>
        public async ETTask<long> CreateFiber(SchedulerType schedulerType, long rootId, int sceneType, string name)
        {
            schedulerType = this.NormalizeChildSchedulerType(schedulerType);
            
            Fiber fiber = await FiberManager.Instance.CreateFiber(schedulerType, this.Zone, rootId, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber.Id;
        }
        
        /// <summary>
        /// 在当前纤程下创建一个指定调度策略（<paramref name="schedulerType"/>）与 <paramref name="zone"/> 的子纤程，并返回新子纤程的 <c>FiberId</c>。
        /// </summary>
        public async ETTask<long> CreateFiber(SchedulerType schedulerType, int zone, long rootId, int sceneType, string name)
        {
            schedulerType = this.NormalizeChildSchedulerType(schedulerType);

            Fiber fiber = await FiberManager.Instance.CreateFiber(schedulerType, zone, rootId, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber.Id;
        }
        
        /// <summary>
        /// 安全地移除并销毁指定 <paramref name="fiberId"/> 的子纤程（会自动递归销毁其所有下属子纤程，并确保切换到目标纤程自己的线程中执行 <see cref="FiberDestroyEvent"/> 与 <c>Dispose</c>）。
        /// </summary>
        public async ETTask RemoveFiber(long fiberId)
        {
            if (!this.children.Remove(fiberId, out Fiber fiber))
            {
                return;
            }
            
            // 整个FiberManager释放了
            IScheduler scheduler = fiber;
            if (FiberManager.Instance == null)
            {
                scheduler.Dispose();
                return;
            }
            
            if (fiber.SchedulerType == SchedulerType.Parent)
            {
                foreach (long child in fiber.children.Keys.ToArray())
                {
                    await fiber.RemoveFiber(child);
                }
                await EventSystem.Instance.PublishAsync(fiber.Root, new FiberDestroyEvent());
                scheduler.Dispose();
                return;
            }
            
            TaskCompletionSource<bool> tcs = new();
            // 要扔到fiber线程执行，否则会出现线程竞争
            fiber.ThreadSynchronizationContext.Post(() =>
            {
                FiberDestroy().Coroutine();
                return;

                async ETTask FiberDestroy()
                {
                    foreach (long child in fiber.children.Keys.ToArray())
                    {
                        await fiber.RemoveFiber(child);
                    }
                    Scene fiberRoot = (scheduler as Fiber).Root;
                    await EventSystem.Instance.PublishAsync(fiberRoot, new FiberDestroyEvent());
                    scheduler.Dispose();
                    tcs.SetResult(true);
                }
            });
            await tcs.Task;
        }

        /// <summary>
        /// 移除并销毁当前纤程下的所有子纤程。
        /// </summary>
        public async ETTask RemoveFibers()
        {
            foreach (long i in this.children.Keys.ToArray())
            {
                await this.RemoveFiber(i);
            }
        }
        
        /// <summary>
        /// 根据 <paramref name="id"/> 获取属于当前纤程直接调度的同线程子纤程（仅限 <see cref="SchedulerType.Parent"/> 类型的子纤程才允许获取实例引用）。
        /// </summary>
        public Fiber GetFiber(long id)
        {
            if (!this.children.TryGetValue(id, out Fiber fiber))
            {
                return null;
            }

            if (fiber.SchedulerType != SchedulerType.Parent)
            {
                return null;
            }
            return fiber;
        }
        
        /// <summary>
        /// 根据名称获取属于当前纤程直接调度的同线程子纤程（仅限 <see cref="SchedulerType.Parent"/> 类型）。
        /// </summary>
        public Fiber GetFiber(string name)
        {
            foreach (Fiber fiber in this.children.Values)
            {
                if (fiber.SchedulerType != SchedulerType.Parent)
                {
                    continue;
                }
                if (fiber.Name != name)
                {
                    continue;
                }

                return fiber;
            }
            return null;
        }
        
        /// <summary>
        /// 在当前纤程内注册一个纤程级局部单例 <typeparamref name="T"/>（无参 Awake）。
        /// </summary>
        public void AddSingleton<T>() where T: ASingleton, ISingletonAwake, new()
        {
            T singleton = new();
            singleton.Awake();
            AddSingleton(singleton);
        }
        
        /// <summary>
        /// 在当前纤程内注册一个带 1 个初始化参数的纤程级局部单例 <typeparamref name="T"/>。
        /// </summary>
        public void AddSingleton<T, A>(A a) where T: ASingleton, ISingletonAwake<A>, new()
        {
            T singleton = new();
            singleton.Awake(a);
            AddSingleton(singleton);
        }
        
        /// <summary>
        /// 在当前纤程内注册一个带 2 个初始化参数的纤程级局部单例 <typeparamref name="T"/>。
        /// </summary>
        public void AddSingleton<T, A, B>(A a, B b) where T: ASingleton, ISingletonAwake<A, B>, new()
        {
            T singleton = new();
            singleton.Awake(a, b);
            AddSingleton(singleton);
        }
        
        /// <summary>
        /// 在当前纤程内注册一个带 3 个初始化参数的纤程级局部单例 <typeparamref name="T"/>。
        /// </summary>
        public void AddSingleton<T, A, B, C>(A a, B b, C c) where T: ASingleton, ISingletonAwake<A, B>, new()
        {
            T singleton = new();
            singleton.Awake(a, b);
            AddSingleton(singleton);
        }

        /// <summary>
        /// 将一个已创建的单例实例挂载到当前纤程的局部单例字典中。
        /// </summary>
        public void AddSingleton<T>(T singleton) where T : ASingleton
        {
            this.singletons ??= new Dictionary<Type, object>();
            this.singletons[typeof(T)] = singleton;
        }

        /// <summary>
        /// 获取指定类型的单例：优先从当前纤程局部单例表中查找，若未找到则回退返回进程级全局单例 <see cref="Singleton{T}.Instance"/>。
        /// </summary>
        public T GetSingleton<T>() where T : Singleton<T>
        {
            if (this.singletons == null)
            {
                return Singleton<T>.Instance;
            }
            
            if (!this.singletons.TryGetValue(typeof(T), out object singleton))
            {
                return Singleton<T>.Instance;
            }
            
            return (T)singleton;
        }

        /// <summary>
        /// 移除当前纤程上的局部单例 <typeparamref name="T"/>。
        /// </summary>
        public bool RemoveSingleton<T>() where T : class
        {
            if (this.singletons == null)
            {
                return false;
            }
            return this.singletons.Remove(typeof(T));
        }

        /// <summary>
        /// 从父纤程继承所有实现了 <see cref="IInheritableSingleton"/> 接口的可继承单例。
        /// </summary>
        internal void InheritSingletonsFrom(Fiber parent)
        {
            if (parent?.singletons == null)
            {
                return;
            }

            foreach ((Type type, object singleton) in parent.singletons)
            {
                if (singleton is not IInheritableSingleton)
                {
                    continue;
                }

                this.singletons ??= new Dictionary<Type, object>();
                this.singletons.TryAdd(type, singleton);
            }
        }

        /// <summary>
        /// 若启动参数指定了强制单线程模式（<c>Options.Instance.SingleThread == 1</c>），则将所有子纤程的调度策略归一化为 <see cref="SchedulerType.Parent"/>。
        /// </summary>
        private SchedulerType NormalizeChildSchedulerType(SchedulerType schedulerType)
        {
            if (Options.Instance.SingleThread == 1)
            {
                return SchedulerType.Parent;
            }

            return schedulerType;
        }

        /// <summary>
        /// 禁止逻辑调用此方法，应该由 <see cref="RemoveFiber"/> 去移除一个 fiber。
        /// </summary>
        void IScheduler.Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            this.IsDisposed = true;
            try
            {
                this.Root.Dispose();
            }
            catch (Exception e)
            {
                this.Log.Error(e);
            }
            
            FiberManager.Instance?.RemoveMonitor(this.Id);
            this.singletons?.Clear();
            
            foreach (long child in this.children.Keys.ToArray())
            {
                this.RemoveFiber(child).Coroutine();
            }
        }

        /// <summary>
        /// 将一个 <see cref="SchedulerType.Parent"/> 类型的子纤程加入当前父纤程的调度队列中。
        /// </summary>
        public void AddToScheduler(Fiber fiber)
        {
            this.schedulerQueue.Enqueue(fiber);
        }
    }
}
