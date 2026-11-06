namespace ET
{
    /// <summary>
    /// 所有 ECS 实体生命周期系统（如 <see cref="AwakeSystem{T}"/>、<see cref="UpdateSystem{T}"/>、<see cref="DestroySystem{T}"/>）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 在 ET 的 ECS 架构中，Entity 只存数据，所有的生命周期回调都被提取为继承自 <see cref="SystemObject"/> 的无状态系统实例，
    /// 由 <see cref="EntitySystemSingleton"/> 在全局统一管理并分发调用。
    /// </remarks>
    public abstract class SystemObject: Object
    {

    }
}