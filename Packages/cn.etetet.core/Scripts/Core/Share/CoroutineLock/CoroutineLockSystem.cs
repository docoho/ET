using System;

namespace ET
{
    /// <summary>
    /// <see cref="CoroutineLock"/> 协程锁凭证实体的生命周期与超时监控系统。
    /// </summary>
    [EntitySystemOf(typeof(CoroutineLock))]
    public static partial class CoroutineLockSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLock self, long type, long k, int count)
        {
            self.type = type;
            self.key = k;
            self.level = count;
        }
        
        /// <summary>
        /// 当业务层离开 <c>using (await Wait(...))</c> 块触发 <c>Dispose()</c> 时执行：
        /// 自动通知所属 <see cref="CoroutineLockComponent"/> 在下一帧唤醒同一 <c>(type, key)</c> 下排队的下一个协程。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this CoroutineLock self)
        {
            self.Scene<CoroutineLockComponent>().RunNextCoroutine(self.type, self.key, self.level + 1);
            self.type = 0;
            self.key = 0;
            self.level = 0;
        }
                
        /// <summary>
        /// 协程锁持锁超时监控任务（同时也是初学者学习 <see cref="EntityRef{T}"/> 标准用法的绝佳范例！）：
        /// <para>
        /// 如果持锁者在 <paramref name="timeout"/> 毫秒内正常完成了 <c>using</c> 块并释放了锁，那么 <c>await timerComponent.WaitAsync(timeout)</c> 结束时，<c>self = selfRef</c> 就会变成 <c>null</c>，什么都不会发生；
        /// 反之，如果 <c>self != null</c>，说明持锁者超时仍未释放锁（可能发生了死锁或网络挂起未设超时），立即打印包含源码文件名与行号的 Error 日志，并强制销毁该锁以唤醒后续排队协程！
        /// </para>
        /// </summary>
        internal static async ETTask SetTimeout(this CoroutineLock self, int timeout, int line, string filePath)
        {
            // 在await前创建EntityRef
            EntityRef<CoroutineLock> selfRef = self;
            long type = self.type;
            long key = self.key;
            int level = self.level;
            
            TimerComponent timerComponent = self.Root().TimerComponent;
            await timerComponent.WaitAsync(timeout);
            
            // await后通过EntityRef重新获取Entity
            self = selfRef;
            
            // 超时后检查锁是否还存在
            if (self != null)
            {
                Log.Error($"{filePath}:{line} Coroutine lock timeout after {timeout}ms, type: {type}, key: {key}, level: {level}");
                selfRef.Dispose();
            }
        }
    }
}