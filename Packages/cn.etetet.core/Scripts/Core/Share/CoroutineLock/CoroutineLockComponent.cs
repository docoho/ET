using System;
using System.Collections.Generic;

namespace ET
{
    public partial class Scene
    {
        private EntityRef<CoroutineLockComponent> coroutineLockComponent;

        /// <summary>
        /// 根场景上挂载的协程锁组件快捷访问属性（避免每次调用 <c>GetComponent&lt;CoroutineLockComponent&gt;()</c> 的哈希查找开销）。
        /// </summary>
        public CoroutineLockComponent CoroutineLockComponent
        {
            get
            {
                return this.coroutineLockComponent;
            }
            set
            {
                this.coroutineLockComponent = value;
            }
        }
    }

    /// <summary>
    /// 协程锁管理组件（挂载在 <see cref="Scene"/> 上，且自身实现了 <see cref="IScene"/>）。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— 单线程为什么还需要“锁”（CoroutineLock）？】
    /// 1. <b>单线程异步下的“重入（Reentrancy）”问题</b>：
    ///    虽然一个 <see cref="Fiber"/> 是单线程执行的，不会有两个线程同时改同一个变量；
    ///    但是当你写了一个 <c>async ETTask BuyItem()</c> 方法，中间有一句 <c>await db.Query(...)</c> 时，当前协程会挂起让出 CPU。
    ///    如果在数据库还没返回这 10 毫秒内，客户端连点按钮又发来第 2 个 <c>BuyItem</c> 请求，第 2 个协程也会进入 <c>BuyItem()</c> 并通过金币检查！结果玩家花一份钱买了两次道具！
    /// 2. <b><c>CoroutineLockComponent</c> 的树形组织结构</b>：
    ///    - 顶层：<see cref="CoroutineLockComponent"/>（按 <c>coroutineLockType</c> 管理子实体 <see cref="CoroutineLockQueueType"/>）
    ///    - 中层：<see cref="CoroutineLockQueueType"/>（按具体业务 <c>key</c> 管理子实体 <see cref="CoroutineLockQueue"/>）
    ///    - 底层：<see cref="CoroutineLockQueue"/>（维护等待该 <c>(type, key)</c> 的 <c>ETTask</c> 队列及当前持有的 <see cref="CoroutineLock"/> 子实体）
    /// 3. <b>为什么释放锁后要放到 <see cref="nextFrameRun"/> 下一帧唤醒？</b>
    ///    为了防止同一个队列里积压大量同步完成的协程锁任务时在一帧内递归唤醒上千次导致卡帧或调用栈过深。
    /// </remarks>
    [DisableGetComponent]
    [ComponentOf(typeof(Scene))]
    public class CoroutineLockComponent: Entity, IAwake, IScene, IUpdate
    {
        public Fiber Fiber { get; set; }
        public int SceneType { get; set; }
        
        /// <summary>
        /// 待在下一帧 Update 中唤醒的协程锁队列通知项：<c>(coroutineLockType, key, level)</c>。
        /// </summary>
        public readonly Queue<(long, long, int)> nextFrameRun = new();
    }
}