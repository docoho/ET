namespace ET
{
    /// <summary>
    /// <see cref="CoroutineLockQueueType"/> 的逻辑系统：按业务 <c>key</c> 查找、按需创建并在空闲时自动回收 <see cref="CoroutineLockQueue"/>。
    /// </summary>
    [EntitySystemOf(typeof(CoroutineLockQueueType))]
    public static partial class CoroutineLockQueueTypeSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueueType self)
        {
        }

        /// <summary>
        /// 获取指定业务 <paramref name="key"/> 的等待队列，若不存在则从对象池创建并挂载。
        /// </summary>
        private static CoroutineLockQueue Get(this CoroutineLockQueueType self, long key)
        {
            return self.GetChild<CoroutineLockQueue>(key) ?? self.AddChildWithId<CoroutineLockQueue, long>(key, self.Id, true);
        }

        private static void Remove(this CoroutineLockQueueType self, long key)
        {
            self.RemoveChild(key);
        }

        internal static void SetMaxConcurrency(this CoroutineLockQueueType self, long key, int maxConcurrency)
        {
            CoroutineLockQueue coroutineLockQueue = self.Get(key);
            coroutineLockQueue.maxConcurrency = maxConcurrency;
        }

        internal static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockQueueType self, long key, int timeout, int line, string filePath)
        {
            CoroutineLockQueue queue = self.Get(key);
            return await queue.Wait(timeout, line, filePath);
        }

        /// <summary>
        /// 唤醒指定 <paramref name="key"/> 队列中的下一个等待协程；若该队列已经没有任何排队协程，则直接移除并回收该 <see cref="CoroutineLockQueue"/>。
        /// </summary>
        internal static void Notify(this CoroutineLockQueueType self, long key, int level)
        {
            CoroutineLockQueue queue = self.Get(key);
            if (queue == null)
            {
                return;
            }
            
            if (queue.Count == 0)
            {
                self.Remove(key);
                return;
            }
            
            queue.Notify(level);
        }
    }
}
