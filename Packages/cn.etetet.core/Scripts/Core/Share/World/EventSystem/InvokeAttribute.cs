namespace ET
{
    /// <summary>
    /// <see cref="AInvokeHandler{A}"/> 回调处理器注册特性。
    /// 通过指定不同的 <see cref="Type"/>（如 <c>TimerInvokeType</c> 或 <c>SceneType</c>），可将同一参数结构体分发给不同的具体处理器类。
    /// </summary>
    public class InvokeAttribute: BaseAttribute
    {
        /// <summary>用于区分同一参数类型下不同回调实现的子类型编号（默认为 0）</summary>
        public long Type { get; }

        public InvokeAttribute(long type = 0)
        {
            this.Type = type;
        }
    }
}