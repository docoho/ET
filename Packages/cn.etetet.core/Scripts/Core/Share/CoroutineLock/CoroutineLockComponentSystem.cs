using System.Runtime.CompilerServices;

namespace ET
{
    /// <summary>
    /// <see cref="CoroutineLockComponent"/> 协程锁组件逻辑系统。
    /// 提供申请协程锁（<see cref="Wait"/>）、设置并发度（<see cref="SetMaxConcurrency"/>）以及每帧批量唤醒排队协程的能力。
    /// </summary>
    [EntitySystemOf(typeof(CoroutineLockComponent))]
    public static partial class CoroutineLockComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockComponent self)
        {
            self.GetParent<Scene>().CoroutineLockComponent = self;
        }
        
        [EntitySystem]
        private static void Update(this CoroutineLockComponent self)
        {
            // 循环过程中会有对象继续加入队列，这里避免死循环，每帧处理当前队列长度的对象
            // 因为协程锁可能是同步处理完成，这样会导致await之后的逻辑立即加入队列，从而导致一帧处理过多
            // 比如map收到发给unit的消息，大部分消息都是同步的，假如一帧收到1000个同步消息，那么之前这里一帧就会处理1000次
            int count = self.nextFrameRun.Count;
            for (int i = 0; i < count; i++)
            {                
                (long coroutineLockType, long key, int level) = self.nextFrameRun.Dequeue();
                self.Notify(coroutineLockType, key, level);
            }       
        }

        /// <summary>
        /// 当上一个 <see cref="CoroutineLock"/> 被释放销毁时调用，将唤醒请求压入 <see cref="CoroutineLockComponent.nextFrameRun"/> 队列，等待下一帧 Update 处理。
        /// </summary>
        internal static void RunNextCoroutine(this CoroutineLockComponent self, long coroutineLockType, long key, int level)
        {
            // 一个协程队列一帧处理超过100个,说明比较多了,打个warning,检查一下是否够正常
            if (level == 100)
            {
                Log.Warning($"too much coroutine level: {coroutineLockType} {key}");
            }

            self.nextFrameRun.Enqueue((coroutineLockType, key, level));
        }

        /// <summary>
        /// 获取或按需创建指定锁类型（<paramref name="coroutineLockType"/>）的 <see cref="CoroutineLockQueueType"/> 节点。
        /// </summary>
        private static CoroutineLockQueueType Get(this CoroutineLockComponent self, long coroutineLockType)
        {
            return self.GetChild<CoroutineLockQueueType>(coroutineLockType) ?? self.AddChildWithId<CoroutineLockQueueType>(coroutineLockType);
        }

        /// <summary>
        /// 控制指定 <c>(coroutineLockType, key)</c> 允许同时持锁并发执行的协程数量上限（默认为 1，即完全互斥；若大于 1 则起到异步信号量 Semaphore 的作用）。
        /// </summary>
        public static void SetMaxConcurrency(this CoroutineLockComponent self, long coroutineLockType, long key, int maxConcurrency)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            coroutineLockQueueType.SetMaxConcurrency(key, maxConcurrency);
        }

        /// <summary>
        /// 异步等待并获取指定 <c>(coroutineLockType, key)</c> 的协程锁，带超时自动释放保护（防止异常死锁）。
        /// <para>
        /// 【标准用法】<c>using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.XXX, key)) { ... }</c>
        /// </para>
        /// </summary>
        /// <param name="self">协程锁组件</param>
        /// <param name="coroutineLockType">锁类型（定义在 <c>CoroutineLockType</c> 中）</param>
        /// <param name="key">锁键值（例如实体 Id）</param>
        /// <param name="timeout">协程锁最长占用毫秒数（默认 30000ms），超时则自动打印错误并强制释放锁以防死锁</param>
        /// <param name="line">编译器自动注入的调用行号（用于超时报错定位）</param>
        /// <param name="filePath">编译器自动注入的调用文件路径（用于超时报错定位）</param>
        /// <returns>协程锁实体的安全引用（配合 <c>using</c> 自动释放）</returns>
        public static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockComponent self, long coroutineLockType, long key, int timeout = 30000, [CallerLineNumber] int line = 0, [CallerFilePath] string filePath = "")
        {
            if (timeout <= 0) // 必须要计时，防止死锁
            {
                timeout = 30000;
            }
            
            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            return await coroutineLockQueueType.Wait(key, timeout, line, filePath);
        }

        private static void Notify(this CoroutineLockComponent self, long coroutineLockType, long key, int level)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.GetChild<CoroutineLockQueueType>(coroutineLockType);
            if (coroutineLockQueueType == null)
            {
                return;
            }
            coroutineLockQueueType.Notify(key, level);
        }
    }
}
