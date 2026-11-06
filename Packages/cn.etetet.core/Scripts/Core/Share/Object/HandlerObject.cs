namespace ET
{
    /// <summary>
    /// 所有消息处理器（MessageHandler）、事件处理器（AEvent）与回调处理器（AInvokeHandler）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 处理器（Handler）在 ET 中是**无状态（Stateless）单例对象**，只承载处理逻辑，严禁在 Handler 派生类中声明与具体业务实体绑定的可变成员字段。
    /// </remarks>
    public abstract class HandlerObject: Object
    {

    }
}