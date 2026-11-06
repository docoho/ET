namespace ET
{
    /// <summary>
    /// 消息包装器接口（当一条内网转发消息内部嵌套包裹了另一条真实业务消息时，实现此接口以便 <see cref="LogMsg"/> 解包打印内部真实消息内容）。
    /// </summary>
    public interface IMessageWrapper
    {
        /// <summary>
        /// 获取被包裹的内部真实消息对象。
        /// </summary>
        IMessage GetMessageObject();
    }
}