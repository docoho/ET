using System;

namespace ET
{
    /// <summary>
    /// ECS 系统类型元数据接口。用于告知 <see cref="EntitySystemSingleton"/> 该 System 服务于哪个 <see cref="Entity"/> 类型以及属于哪种生命周期接口。
    /// </summary>
    public interface ISystemType
    {
        /// <summary>返回该 System 所绑定的目标 <see cref="Entity"/> 派生类型（如 <c>typeof(Unit)</c>）</summary>
        Type Type();
        /// <summary>返回该 System 实现的系统调度接口类型（如 <c>typeof(IAwakeSystem)</c>、<c>typeof(AEventSystem&lt;UpdateEvent&gt;)</c>）</summary>
        Type SystemType();
    }
}