namespace ET
{
    /// <summary>
    /// <see cref="CoroutineLockQueue"/> 协程锁队列逻辑系统：负责判断当前并发数是否已满、入队挂起等待以及出队唤醒。
    /// </summary>
    [EntitySystemOf(typeof(CoroutineLockQueue))]
    public static partial class CoroutineLockQueueSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueue self, long type)
        {
            self.type = type;
            self.runningCount = 0;
            self.maxConcurrency = 1;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLockQueue self)
        {
            self.queue.Clear();
            self.type = 0;
            self.maxConcurrency = 0;
            self.runningCount = 0;
        }
        
        /// <summary>
        /// 申请获取当前队列的协程锁：
        /// - 若当前运行数 <c>runningCount &lt; maxConcurrency</c>，无需等待，立即创建并返回 <see cref="CoroutineLock"/>；
        /// - 否则创建 <see cref="ETTask{T}"/> 压入等待队列挂起，直到前一个持锁协程释放锁并在下一帧调用 <see cref="Notify"/> 唤醒它。
        /// </summary>
        internal static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockQueue self, int timeout, int line, string filePath)
        {
            CoroutineLock coroutineLock = null;
            if (self.runningCount < self.maxConcurrency)
            {
                ++self.runningCount;
                coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.type, self.Id, 1);
            }
            else
            {
                ETTask<EntityRef<CoroutineLock>> tcs = ETTask<EntityRef<CoroutineLock>>.Create(true);
                self.queue.Enqueue(tcs);
                coroutineLock = await tcs;
            }

            // 成功拿到锁之后才开始计算持锁超时时间（避免排队等待时间被误算进持锁超时）
            coroutineLock.SetTimeout(timeout, line, filePath).Coroutine();

            return coroutineLock;
        }

        /// <summary>
        /// 当前一个持锁者释放锁后调用：递减 <c>runningCount</c>，并在未达最大并发数前依次出队唤醒等待中的协程。
        /// </summary>
        // 返回值，是否找到了一个有效的协程锁
        internal static void Notify(this CoroutineLockQueue self, int level)
        {
            --self.runningCount;
            
            // 尝试唤醒等待队列中的协程，直到达到并发上限
            while (self.queue.Count > 0)
            {
                // 达到最大并发数量
                if (self.runningCount >= self.maxConcurrency)
                {
                    break;
                }
                ++self.runningCount;
                
                ETTask<EntityRef<CoroutineLock>> tcs = self.queue.Dequeue();
                CoroutineLock coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.type, self.Id, level);
                tcs.SetResult(coroutineLock);
            }
        }
    }
}