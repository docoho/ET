using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// <see cref="TimerAction"/> 实体的生命周期与辅助系统。
    /// </summary>
    [EntitySystemOf(typeof(TimerAction))]
    public static partial class TimerActionSystem
    {
        [EntitySystem]
        private static void Awake(this TimerAction self)
        {
        }

        /// <summary>
        /// 销毁定时器实体时重置所有字段，并释放池化的包装器对象（如 <see cref="ValueTypeWrap{T}"/>）。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this TimerAction self)
        {
            self.TimerClass = TimerClass.None;
            self.StartTime = 0;
            self.Time = 0;
            self.Type = 0;

            if (self.Object is IDisposable disposable)
            {
                disposable.Dispose();
            }

            self.Object = null;
        }

        /// <summary>
        /// 从 <see cref="TimerAction.Object"/> 中解包并校验关联的 <see cref="Entity"/>（若实体已销毁则返回 null）。
        /// </summary>
        internal static Entity GetEntity(this TimerAction self)
        {
            var wrap = (ValueTypeWrap<EntityRef<Entity>>)self.Object;
            return wrap.Value;
        }
    }

    /// <summary>
    /// <see cref="TimerComponent"/> 的系统逻辑实现，提供定时轮询驱动、异步延时等待与周期回调注册 API。
    /// </summary>
    [EntitySystemOf(typeof(TimerComponent))]
    public static partial class TimerComponentSystem
    {
        /// <summary>
        /// 初始化时将自身注册到所属 <see cref="Scene.TimerComponent"/> 属性上，方便通过 <c>self.Scene().TimerComponent</c> 访问。
        /// </summary>
        [EntitySystem]
        private static void Awake(this TimerComponent self)
        {
            self.GetParent<Scene>().TimerComponent = self;
        }

        /// <summary>
        /// 每帧轮询检查是否有定时器到期，并依次触发所有已到期的定时器。
        /// </summary>
        [EntitySystem]
        private static void Update(this TimerComponent self)
        {
            if (self.timeId.Count == 0)
            {
                return;
            }

            long timeNow = self.GetNow();

            // 快速剪枝：如果当前时间还未达到最早到期时间 minTime，直接返回（O(1)）
            if (timeNow < self.minTime)
            {
                return;
            }

            // 1. 遍历有序字典，收集所有小于等于当前时间的时间戳，并更新下一个最早到期时间 minTime
            foreach (var kv in self.timeId)
            {
                long k = kv.Key;
                if (k > timeNow)
                {
                    self.minTime = k;
                    break;
                }

                self.timeOutTime.Enqueue(k);
            }

            // 2. 将所有到期时间戳对应的 TimerId 转移到待执行队列，并从 timeId 字典中移除这些时间戳
            while (self.timeOutTime.Count > 0)
            {
                long time = self.timeOutTime.Dequeue();
                List<long> list = self.timeId[time];
                for (int i = 0; i < list.Count; ++i)
                {
                    long timerId = list[i];
                    self.timeOutTimerIds.Enqueue(timerId);
                }

                self.timeId.Remove(time);
            }

            if (self.timeId.Count == 0)
            {
                self.minTime = long.MaxValue;
            }

            // 3. 逐个执行到期的定时器（先出队再执行，防止回调中又注册新定时器影响集合遍历）
            while (self.timeOutTimerIds.Count > 0)
            {
                long timerId = self.timeOutTimerIds.Dequeue();
                self.Run(timerId);
            }
        }

        /// <summary>
        /// 获取当前服务器同步时间戳（毫秒）。
        /// </summary>
        private static long GetNow(this TimerComponent self)
        {
            return self.GetSingleton<TimeInfo>().ServerNow();
        }

        /// <summary>
        /// 执行指定 Id 的到期定时器。
        /// </summary>
        private static void Run(this TimerComponent self, long timerId)
        {
            // 若定时器在到期前已被调用 Remove 销毁，则 GetChild 返回 null，此处自动跳过
            TimerAction timerAction = self.GetChild<TimerAction>(timerId);
            if (timerAction == null)
            {
                return;
            }

            switch (timerAction.TimerClass)
            {
                case TimerClass.OnceTimer:
                {
                    Entity entity = timerAction.GetEntity();
                    int timerActionType = timerAction.Type;
                    self.RemoveChild(timerId);

                    if (entity != null)
                    {
                        EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    }                    
                    break;
                }
                case TimerClass.OnceWaitTimer:
                {
                    ETTask tcs = timerAction.Object as ETTask;
                    self.RemoveChild(timerId);
                    tcs.SetResult();
                    break;
                }
                case TimerClass.RepeatedTimer:
                {
                    int timerActionType = timerAction.Type;
                    Entity entity = timerAction.GetEntity();

                    // 若关联的实体仍然存活，则重新设定起始时间并排入下一周期；若实体已销毁，则自动清理该周期定时器
                    if (entity != null)
                    {
                        timerAction.StartTime = self.GetNow();
                        self.AddTimer(timerAction);
                        EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    }
                    else
                    {
                        self.RemoveChild(timerId);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 从对象池创建并初始化一个 <see cref="TimerAction"/> 子实体，同时将其加入时间轴字典。
        /// </summary>
        private static TimerAction CreateTimerAction(this TimerComponent self, TimerClass timerClass, long startTime, long time, int type, object obj)
        {
            TimerAction timer = self.AddChild<TimerAction>(true);
            timer.TimerClass = timerClass;
            timer.StartTime = startTime;
            timer.Object = obj;
            timer.Time = time;
            timer.Type = type;

            self.AddTimer(timer);
            return timer;
        }

        /// <summary>
        /// 计算定时器的到期时间戳，插入 <see cref="TimerComponent.timeId"/> 并维护 <see cref="TimerComponent.minTime"/>。
        /// </summary>
        private static void AddTimer(this TimerComponent self, TimerAction timer)
        {
            long tillTime = timer.StartTime + timer.Time;
            self.timeId.Add(tillTime, timer.Id);
            if (tillTime < self.minTime)
            {
                self.minTime = tillTime;
            }
        }

        /// <summary>
        /// 根据引用传入的定时器 Id 取消并移除定时器，同时将传入的 <paramref name="id"/> 清零（防止重复移除）。
        /// </summary>
        public static bool Remove(this TimerComponent self, ref long id)
        {
            long i = id;
            id = 0;
            return self.Remove(i);
        }

        /// <summary>
        /// 根据定时器 Id 取消并移除对应的 <see cref="TimerAction"/> 子实体。
        /// </summary>
        private static bool Remove(this TimerComponent self, long id)
        {
            if (id == 0)
            {
                return false;
            }

            return self.RemoveChild(id);
        }

        /// <summary>
        /// 异步等待直到指定的绝对时间戳（毫秒），支持通过协程上下文中的 <see cref="ETCancellationToken"/> 提前取消。
        /// </summary>
        /// <param name="self">定时器组件</param>
        /// <param name="tillTime">目标绝对时间戳（毫秒）</param>
        public static async ETTask WaitTillAsync(this TimerComponent self, long tillTime)
        {
            long timeNow = self.GetNow();
            if (timeNow >= tillTime)
            {
                return;
            }

            ETTask tcs = ETTask.Create(true);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceWaitTimer, timeNow, tillTime - timeNow, 0, tcs);
            long timerId = timer.Id;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            try
            {
                cancellationToken?.Add(CancelAction);
                await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }

            return;

            void CancelAction()
            {
                if (!self.Remove(timerId))
                {
                    return;
                }

                tcs.SetResult();
            }
        }

        /// <summary>
        /// 异步等待一帧（内部等价于等待 1 毫秒，在下一帧 Update 时唤醒）。
        /// </summary>
        public static async ETTask WaitFrameAsync(this TimerComponent self)
        {
            await self.WaitAsync(1);
        }

        /// <summary>
        /// 异步等待指定的毫秒时长，支持通过协程上下文中的 <see cref="ETCancellationToken"/> 提前取消。
        /// </summary>
        /// <param name="self">定时器组件</param>
        /// <param name="time">需要等待的毫秒数</param>
        public static async ETTask WaitAsync(this TimerComponent self, long time)
        {
            if (time == 0)
            {
                return;
            }

            long timeNow = self.GetNow();

            ETTask tcs = ETTask.Create(true);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceWaitTimer, timeNow, time, 0, tcs);
            long timerId = timer.Id;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            try
            {
                cancellationToken?.Add(CancelAction);
                await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }

            return;

            void CancelAction()
            {
                if (!self.Remove(timerId))
                {
                    return;
                }

                tcs.SetResult();
            }
        }

        // 用这个优点是可以热更，缺点是回调式的写法，逻辑不连贯。WaitTillAsync不能热更，优点是逻辑连贯。
        // wait时间短并且逻辑需要连贯的建议WaitTillAsync
        // wait时间长不需要逻辑连贯的建议用NewOnceTimer
        /// <summary>
        /// 创建一个在指定绝对时间戳触发的单次回调定时器。
        /// </summary>
        /// <param name="self">定时器组件</param>
        /// <param name="tillTime">触发的绝对时间戳（毫秒）</param>
        /// <param name="type">回调事件类型（<c>TimerInvokeType</c>）</param>
        /// <param name="args">传递给回调处理器的目标实体</param>
        /// <returns>创建的定时器 Id（可用于调用 <see cref="Remove(TimerComponent, ref long)"/> 取消）</returns>
        public static long NewOnceTimer(this TimerComponent self, long tillTime, int type, Entity args)
        {
            long timeNow = self.GetNow();

            EntityRef<Entity> entityRef = args;
            ValueTypeWrap<EntityRef<Entity>> wrap = ValueTypeWrap<EntityRef<Entity>>.Create(entityRef);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceTimer, timeNow, tillTime - timeNow, type, wrap);
            return timer.Id;
        }

        /// <summary>
        /// 创建一个每帧（服务端为每 100ms，客户端为每帧 0ms）触发的周期回调定时器。
        /// </summary>
        public static long NewFrameTimer(this TimerComponent self, int type, Entity args)
        {
#if DOTNET
            return self.NewRepeatedTimerInner(100, type, args);
#else
            return self.NewRepeatedTimerInner(0, type, args);
#endif
        }

        /// <summary>
        /// 创建一个RepeatedTimer
        /// </summary>
        private static long NewRepeatedTimerInner(this TimerComponent self, long time, int type, Entity args)
        {
#if DOTNET
            if (time < 50)
            {
                throw new Exception($"repeated timer < 50, timerType: time: {time}");
            }
#endif

            long timeNow = self.GetNow();
            EntityRef<Entity> entityRef = args;
            ValueTypeWrap<EntityRef<Entity>> wrap = ValueTypeWrap<EntityRef<Entity>>.Create(entityRef);
            TimerAction timer = self.CreateTimerAction(TimerClass.RepeatedTimer, timeNow, time, type, wrap);
            return timer.Id;
        }

        /// <summary>
        /// 创建一个按固定时间间隔（毫秒）重复触发的周期回调定时器（服务端要求间隔不少于 50ms）。
        /// </summary>
        /// <param name="self">定时器组件</param>
        /// <param name="time">触发间隔时长（毫秒）</param>
        /// <param name="type">回调事件类型（<c>TimerInvokeType</c>）</param>
        /// <param name="args">传递给回调处理器的目标实体（若实体销毁，定时器将在下次触发时自动清理）</param>
        /// <returns>创建的定时器 Id</returns>
        public static long NewRepeatedTimer(this TimerComponent self, long time, int type, Entity args)
        {
            return self.NewRepeatedTimerInner(time, type, args);
        }
    }
}
