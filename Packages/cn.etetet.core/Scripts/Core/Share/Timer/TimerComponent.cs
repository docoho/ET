using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 定时器类型枚举。
    /// </summary>
    public enum TimerClass
    {
        /// <summary>
        /// 无效/初始状态。
        /// </summary>
        None,
        /// <summary>
        /// 单次回调定时器（到期后通过 <c>EventSystem.Invoke</c> 触发一次回调，支持热重载）。
        /// </summary>
        OnceTimer,
        /// <summary>
        /// 单次异步等待定时器（到期后唤醒 <see cref="ETTask"/>，用于 <c>WaitAsync</c> / <c>WaitTillAsync</c>）。
        /// </summary>
        OnceWaitTimer,
        /// <summary>
        /// 周期重复回调定时器（每次到期触发回调并自动重新排入下一周期，直到手动移除或关联实体销毁）。
        /// </summary>
        RepeatedTimer,
    }

    /// <summary>
    /// 单个定时器任务实体，作为 <see cref="TimerComponent"/> 的子实体（Child）存在并启用对象池复用。
    /// <remarks>
    /// 将每个定时器设计为 <see cref="Entity"/> 的好处是：可以直接利用实体的唯一 <c>Id</c> 作为定时器句柄（TimerId），
    /// 删除定时器只需 <c>RemoveChild(timerId)</c>，即使到期时间字典里还残留旧 Id，到期取出时查不到子实体也会安全忽略。
    /// </remarks>
    /// </summary>
    [ChildOf(typeof(TimerComponent))]
    public partial class TimerAction: Entity, IAwake, IDestroy, IPool
    {
        /// <summary>
        /// 定时器类别（单次回调、单次异步等待、周期重复回调）。
        /// </summary>
        public TimerClass TimerClass;
        
        /// <summary>
        /// 回调事件类型（对应 <c>TimerInvokeType</c>，仅在回调式定时器中使用）。
        /// </summary>
        public int Type;

        /// <summary>
        /// 附加上下文对象：
        /// 对于 <see cref="TimerClass.OnceWaitTimer"/>，存放的是 <see cref="ETTask"/>；
        /// 对于 <see cref="TimerClass.OnceTimer"/> 和 <see cref="TimerClass.RepeatedTimer"/>，存放的是 <c>ValueTypeWrap&lt;EntityRef&lt;Entity&gt;&gt;</c>。
        /// </summary>
        public object Object;

        /// <summary>
        /// 定时器本轮计时的起始时间戳（毫秒）。
        /// </summary>
        public long StartTime;

        /// <summary>
        /// 定时器的延迟时长或定时间隔（毫秒）。
        /// </summary>
        public long Time;
    }

    /// <summary>
    /// 定时器触发时传递给 <see cref="ATimer{T}"/> 的事件参数结构体。
    /// </summary>
    public struct TimerCallback
    {
        /// <summary>
        /// 关联实体的安全弱引用（若实体已销毁，解引用将返回 null）。
        /// </summary>
        public EntityRef<Entity> Args;
    }

    public partial class Scene
    {
        private EntityRef<TimerComponent> timerComponent;

        /// <summary>
        /// 当前场景挂载的定时器组件快捷访问属性。
        /// </summary>
        public TimerComponent TimerComponent
        {
            get
            {
                return this.timerComponent;
            }
            set
            {
                this.timerComponent = value;
            }
        }
    }

    /// <summary>
    /// 定时器管理组件，挂载在 <see cref="Scene"/> 上，驱动当前场景/纤程内所有的定时等待与周期回调任务。
    /// <remarks>
    /// 底层采用有序映射表 <see cref="MultiMap{T, K}"/>（以到期时间戳为 Key，TimerId 列表为 Value）结合最小时间戳缓存 <see cref="minTime"/>，
    /// 使得未到期帧的 <c>Update</c> 开销仅为一次 <c>long</c> 比较（O(1)）。
    /// </remarks>
    /// </summary>
    [DisableGetComponent]
    [SkipAwaitEntityCheck]
    [ComponentOf(typeof(Scene))]
    public class TimerComponent: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 按到期时间戳排序的多值字典（key: 到期时间戳 tillTime, value: 在该时刻到期的 TimerAction.Id 列表）。
        /// </summary>
        public readonly MultiMap<long, long> timeId = new(1000);

        /// <summary>
        /// 本帧已到期的时间戳临时队列（避免在遍历 <see cref="timeId"/> 时直接修改集合）。
        /// </summary>
        public readonly Queue<long> timeOutTime = new();

        /// <summary>
        /// 本帧所有待执行的到期定时器 Id 队列。
        /// </summary>
        public readonly Queue<long> timeOutTimerIds = new();
        
        // 记录最小时间，不用每次都去MultiMap取第一个值
        public long minTime = long.MaxValue;
    }
}
