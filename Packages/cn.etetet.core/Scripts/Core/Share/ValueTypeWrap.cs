namespace ET
{
    /// <summary>
    /// 值类型包装器抽象基类。
    /// <remarks>
    /// 用于在需要将结构体（如 <see cref="EntityRef{T}"/>）存入 <c>object</c> 字段时，配合对象池避免频繁装箱（Boxing）产生 GC 垃圾。
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">被包装的值类型（struct）</typeparam>
    public abstract class IValue<T>: DisposeObject where T: struct
    {
        /// <summary>
        /// 获取或设置被包装的值类型数据。
        /// </summary>
        public T Value
        {
            get; set;
        }
    }
    
    /// <summary>
    /// 支持对象池复用的值类型包装类。
    /// <remarks>
    /// 典型场景：在 <see cref="TimerAction"/> 中保存 <see cref="EntityRef{Entity}"/> 到 <c>object</c> 字段时，
    /// 使用 <c>ValueTypeWrap&lt;EntityRef&lt;Entity&gt;&gt;.Create(entityRef)</c> 从对象池取出包装器，定时器销毁时调用 <c>Dispose()</c> 自动回收到池中，实现零 GC 分配。
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">被包装的值类型（struct）</typeparam>
    [EnableClass]
    public class ValueTypeWrap<T>: IValue<T>, IPool where T: struct
    {
        /// <summary>
        /// 从对象池创建并初始化一个值类型包装器实例。
        /// </summary>
        /// <param name="value">要包装的结构体值</param>
        /// <param name="isFromPool">是否从对象池获取（默认 true）</param>
        public static ValueTypeWrap<T> Create(T value, bool isFromPool = true)
        {
            ValueTypeWrap<T> vw = ObjectPool.Fetch<ValueTypeWrap<T>>(isFromPool);
            vw.Value = value;
            return vw;
        }

        /// <summary>
        /// 标记当前实例是否来自对象池。
        /// </summary>
        public bool IsFromPool { get; set; }

        /// <summary>
        /// 回收进对象池时清空内部值。
        /// </summary>
        void IPool.Clear()
        {
            this.Value = default;
        }
    }
}
