namespace ET
{
    /// <summary>
    /// 可继承纤程单例标记接口。
    /// <para>
    /// 当一个 <see cref="Fiber"/> 上的单例实现了 <see cref="IInheritableSingleton"/> 接口时，
    /// 由该父 Fiber 创建出的子 Fiber（参见 <see cref="FiberManager"/>）会自动继承/共享父 Fiber 上的这一个单例引用（如配置表单例），无需在每个子 Fiber 中重复加载。
    /// </para>
    /// </summary>
    public interface IInheritableSingleton
    {
    }
}
