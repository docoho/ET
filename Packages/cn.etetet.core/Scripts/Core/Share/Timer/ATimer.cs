namespace ET
{
    /// <summary>
    /// 定时器回调处理器抽象基类（基于 <see cref="EventSystem"/> 的 <c>Invoke</c> 分发机制）。
    /// <remarks>
    /// 当使用 <c>NewOnceTimer</c>、<c>NewRepeatedTimer</c> 或 <c>NewFrameTimer</c> 注册回调式定时器时，
    /// 需定义一个继承自 <see cref="ATimer{T}"/> 的类并标记 <c>[Invoke(TimerInvokeType.XXX)]</c> 特性。
    /// 相比 <c>WaitAsync</c>，回调式定时器的优势是支持热更新（修改 <see cref="Run"/> 逻辑后热重载即可生效）。
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">定时器关联的目标实体类型</typeparam>
    public abstract class ATimer<T>: AInvokeHandler<TimerCallback> where T: Entity
    {
        /// <summary>
        /// 接收 <see cref="TimerCallback"/> 事件，解包出目标实体并强转后调用子类的 <see cref="Run"/> 方法。
        /// </summary>
        public override void Handle(TimerCallback a)
        {
            this.Run(a.Args.Entity as T);
        }

        /// <summary>
        /// 定时器触发时执行的具体业务逻辑（由子类实现）。
        /// </summary>
        /// <param name="t">触发定时器的实体对象</param>
        protected abstract void Run(T t);
    }
}