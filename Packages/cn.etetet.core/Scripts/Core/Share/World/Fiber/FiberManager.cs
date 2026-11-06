using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    /// <summary>
    /// <see cref="Fiber"/> 纤程的线程调度策略类型枚举。
    /// </summary>
    public enum SchedulerType
    {
        /// <summary>跟随父纤程在同一个线程上下文中同步调度（共享父 Fiber 的 ThreadSynchronizationContext）</summary>
        Parent = -1,
        /// <summary>在进程主线程（或 Unity 主线程）中调度（由 <see cref="MainThreadScheduler"/> 驱动）</summary>
        Main = 0,
        /// <summary>为该纤程单独开启一个独占的操作系统物理线程进行调度（由 <see cref="ThreadScheduler"/> 驱动，适合极高负载的核心 Fiber）</summary>
        Thread = 1,
        /// <summary>放入固定等于 CPU 核心数的线程池中负载均衡调度（由 <see cref="ThreadPoolScheduler"/> 驱动，每次帧循环保证同一时刻只有一个线程执行该 Fiber）</summary>
        ThreadPool = 2,
    }
    
    /// <summary>
    /// 纤程每帧耗时监控信息结构体（记录 Update 与 LateUpdate 耗时毫秒数）。
    /// </summary>
    public struct FiberMonitorInfo
    {
        public string Name;

        public int UpdateTimeUsed;
        
        public int LateUpdateTimeUsed;
        
        public FiberMonitorInfo(string name)
        {
            this.Name = name;
            this.UpdateTimeUsed = 0;
            this.LateUpdateTimeUsed = 0;
        }
    }
    
    /// <summary>
    /// 全局纤程管理器单例（负责所有 <see cref="Fiber"/> 的创建、ID 槽位分配、调度器挂载以及主线程帧循环驱动）。
    /// </summary>
    [AllowInstance]
    public class FiberManager: Singleton<FiberManager>, ISingletonAwake
    {
        private readonly object localSlotLock = new();

        private readonly Dictionary<int, int> nextLocalSlotByZone = new();
        
        /// <summary>三种顶层调度器实例数组（索引对应 <see cref="SchedulerType.Main"/>、<see cref="SchedulerType.Thread"/>、<see cref="SchedulerType.ThreadPool"/>）</summary>
        private readonly IScheduler[] schedulers = new IScheduler[3];
        
        private MainThreadScheduler mainThreadScheduler;

        /// <summary>进程的主纤程实例（在客户端为 Client 主纤程，在服务端为启动入口主纤程）</summary>
        private Fiber mainFiber;
        
        private readonly ThreadSynchronizationContext context = new();
        
        private readonly ConcurrentDictionary<long, FiberMonitorInfo> fiberMonitorInfos = new();
        
        /// <summary>
        /// 上报并更新指定纤程的单帧耗时监控数据。
        /// </summary>
        public void AddMonitor(long fiberId, ref FiberMonitorInfo fiberMonitorInfo)
        {
            //if (fiberMonitorInfo.UpdateTimeUsed + fiberMonitorInfo.LateUpdateTimeUsed > 5)
            //{
            //    Log.Warning($"FiberMonitor Warn! FiberId: {fiberId} Name: {fiberMonitorInfo.Name} UpdateTimeUsed: {fiberMonitorInfo.UpdateTimeUsed} LateUpdateTimeUsed: {fiberMonitorInfo.LateUpdateTimeUsed}");
            //}
#if !UNITY  // 仅在非Unity环境下启用纤程监控，Unity下这个方法有GC开销
            this.fiberMonitorInfos[fiberId] = fiberMonitorInfo;
#endif
        }
        
        public void RemoveMonitor(long fiberId)
        {
#if !UNITY
            this.fiberMonitorInfos.TryRemove(fiberId, out _);
#endif
        }
        
        public void Awake()
        {
            SynchronizationContext.SetSynchronizationContext(context);
            
            this.mainThreadScheduler = new MainThreadScheduler();
            this.schedulers[(int)SchedulerType.Main] = this.mainThreadScheduler;

            if (Options.Instance.SingleThread != 1)
            {
                this.schedulers[(int)SchedulerType.Thread] = new ThreadScheduler();
                this.schedulers[(int)SchedulerType.ThreadPool] = new ThreadPoolScheduler();
            }
        }

        /// <summary>
        /// 重写销毁顺序为 <c>0</c>，确保在 <see cref="World.Dispose"/> 时最先停止所有纤程调度器与业务实体，然后再销毁底层基础设施单例。
        /// </summary>
        public override int RemoveOrder()
        {
            return 0;
        }
        
        /// <summary>
        /// 主线程每帧 Update 驱动入口（由 Unity <c>Init.Update</c> 或服务端主循环调用）。
        /// </summary>
        public void Update()
        {
            this.context.Update();
            
            this.mainThreadScheduler.Update();
            
            // unity的回调需要用到Instance
            Fiber.Instance = this.mainFiber;
        }

        /// <summary>
        /// 主线程每帧 LateUpdate 驱动入口（由 Unity <c>Init.LateUpdate</c> 或服务端主循环调用）。
        /// </summary>
        public void LateUpdate()
        {
            this.mainThreadScheduler.LateUpdate();
            
            // unity的回调需要用到Instance
            Fiber.Instance = this.mainFiber;
        }

        protected override void Destroy()
        {
            // 把调度器全部停止
            foreach (IScheduler scheduler in this.schedulers)
            {
                scheduler?.Dispose();
            }
            (this.mainFiber as IScheduler)?.Dispose();
        }

        /// <summary>
        /// 创建进程唯一的顶层主纤程（<see cref="mainFiber"/>），调度在主线程（<see cref="SchedulerType.Main"/>）上。
        /// </summary>
        public async ETTask<long> CreateMainFiber(int sceneType, string sceneName)
        {
            if (this.mainFiber != null)
            {
                throw new Exception("FiberManager is already created");
            }
            this.mainFiber = await this.CreateFiber(SchedulerType.Main, 0, IdGenerater.Instance.GenerateId(), sceneType, sceneName, null);
            return this.mainFiber.Id;
        }

        /// <summary>
        /// 内部核心创建纤程方法：
        /// 1. 实例化 <see cref="Fiber"/> 并创建其根场景 <c>Root</c>；
        /// 2. 将新纤程加入对应的 <see cref="IScheduler"/> 调度队列；
        /// 3. 将 <see cref="FiberInit"/> 事件投递到该新纤程专属的 <see cref="ThreadSynchronizationContext"/> 中执行，确保对应 <c>SceneType</c> 的初始化逻辑（如 <c>FiberInit_Main</c>、<c>FiberInit_Gate</c>）严格在目标纤程自己的线程中完成！
        /// </summary>
        /// <param name="schedulerType">纤程调度器类型</param>
        /// <param name="fiberId">编码后的运行时 64 位纤程 ID</param>
        /// <param name="rootId">根 Scene 的业务 Id</param>
        /// <param name="zone">所属区服编号</param>
        /// <param name="sceneType">场景类型（决定触发哪个 <c>[Invoke(SceneType.XXX)]</c> 的 <c>FiberInit</c> 处理器）</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent">父纤程实例</param>
        private async ETTask<Fiber> CreateFiberInternal(long fiberId, SchedulerType schedulerType, long rootId, int zone, int sceneType,
            string name, Fiber parent)
        {
            if (sceneType == 0)
            {
                throw new Exception("fiberId is 0");
            }
            try
            {
                long parentId = parent?.Id ?? 0;
                Log.Debug($"create fiber: {name} {fiberId} {zone} {sceneType} {schedulerType} {parentId}");
                
                // 如果调度器是父fiber，那么日志也是父fiber的日志
                Fiber fiber = new(fiberId, rootId, sceneType, name, schedulerType, parent);
                if (schedulerType == SchedulerType.Parent)
                {
                    fiber.InheritSingletonsFrom(parent);
                }

                IScheduler iScheduler = schedulerType == SchedulerType.Parent ? parent : this.schedulers[(int)schedulerType];
                iScheduler.AddToScheduler(fiber);
                
                TaskCompletionSource<bool> tcs = new ();

                fiber.ThreadSynchronizationContext.Post(() =>
                {
                    Action().Coroutine();
                });
                await tcs.Task;
                
                Log.Debug($"create fiber {name} finish");
                return fiber;

                async ETTask Action()
                {
                    try
                    {
                        // 根据Fiber的SceneType分发Init,必须在Fiber线程中执行
                        await EventSystem.Instance.Invoke<FiberInit, ETTask>(sceneType, new FiberInit() {Fiber = fiber});
                        tcs.SetResult(true);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(new Exception($"init fiber fail: {sceneType}", e));
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"create fiber error: {fiberId} {sceneType}", e);
            }
        }

        /// <summary>
        /// 为指定 <paramref name="zone"/> 线程安全地分配一个进程内唯一的动态局部槽位号（LocalSlot）。
        /// </summary>
        private int AllocateLocalSlot(int zone)
        {
            FiberIdHelper.ValidateZone(zone);

            lock (this.localSlotLock)
            {
                int localSlot = this.nextLocalSlotByZone.TryGetValue(zone, out int nextLocalSlot)
                        ? nextLocalSlot
                        : FiberIdHelper.DynamicLocalSlotStart;

                if (localSlot >= FiberIdHelper.ReservedLocalSlotStart)
                {
                    throw new Exception(
                        $"fiber local slot exhausted, zone: {zone}, nextLocalSlot: {localSlot}, dynamic range: [{FiberIdHelper.DynamicLocalSlotStart}, {FiberIdHelper.ReservedLocalSlotStart - 1}]");
                }

                this.nextLocalSlotByZone[zone] = localSlot + 1;
                return localSlot;
            }
        }

        /// <summary>
        /// 分配编码后的 FiberId 并创建初始化一个新的 <see cref="Fiber"/>。
        /// </summary>
        /// <param name="schedulerType">纤程调度器类型</param>
        /// <param name="zone">区服号</param>
        /// <param name="rootId">根场景 Id</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent">父纤程</param>
        /// <returns>初始化完成的 <see cref="Fiber"/> 实例</returns>
        internal async ETTask<Fiber> CreateFiber(SchedulerType schedulerType, int zone, long rootId, int sceneType, string name, Fiber parent)
        {
            int localSlot = this.AllocateLocalSlot(zone);
            long fiberId = FiberIdHelper.Encode(zone, localSlot);
            return await this.CreateFiberInternal(fiberId, schedulerType, rootId, zone, sceneType, name, parent);
        }
    }
}
