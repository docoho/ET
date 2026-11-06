namespace ET
{
    /// <summary>
    /// 所有网络协议消息（Proto 生成的消息类）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 为什么标记了 <see cref="DisableNewAttribute"/>？
    /// 网络消息收发极其频繁，为了消除 GC（垃圾回收）抖动，ET 要求网络消息优先通过 <c>.Create()</c> 从对象池创建，
    /// 因此通过编译器分析器禁止业务层直接使用 <c>new</c> 关键字实例化消息对象。
    /// </remarks>
    [DisableNew]
    public abstract class MessageObject: DisposeObject, IMessage
    {
    }
}
