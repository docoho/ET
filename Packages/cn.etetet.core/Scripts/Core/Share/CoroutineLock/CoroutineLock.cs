using System;

namespace ET
{
    /// <summary>
    /// 协程锁凭证实体（挂载在 <see cref="CoroutineLockQueue"/> 下）。
    /// <para>
    /// 【初学者指南】
    /// 当调用 <c>using (await root.CoroutineLockComponent.Wait(lockType, key))</c> 获得锁时，实际上返回的就是本实体的 <see cref="EntityRef{CoroutineLock}"/>。
    /// 当 <c>using</c> 代码块结束触发 <c>Dispose()</c> 时，本实体被销毁，在其 <see cref="CoroutineLockSystem.Destroy"/> 中会自动将下一个排队的协程加入下一帧唤醒队列！
    /// </para>
    /// </summary>
    [ChildOf(typeof(CoroutineLockQueue))]
    public class CoroutineLock: Entity, IAwake<long, long, int>, IDestroy
    {
        /// <summary>协程锁类型（对应 <c>CoroutineLockType</c> 中的常量，如 Mailbox、DB、Location 等）</summary>
        internal long type;
        /// <summary>加锁的具体业务键值（例如玩家 UnitId、ActorId 等，不同 key 之间互不阻塞）</summary>
        internal long key;
        /// <summary>当前锁队列连续传递的深度层级（用于在重度排队超过 100 层时输出告警日志）</summary>
        internal int level;
    }
}