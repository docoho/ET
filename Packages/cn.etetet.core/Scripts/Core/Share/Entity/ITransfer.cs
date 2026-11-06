namespace ET
{
    /// <summary>
    /// 跨服/跨场景传送标记接口。
    /// 当 <c>Unit</c> 在不同 Map 服务器之间进行跨服传送（Transfer）时，只有实现了 <see cref="ITransfer"/> 接口的组件才会跟随 <c>Unit</c> 一起序列化传输到目标服务器。
    /// </summary>
    // Unit的组件有这个接口说明需要传送
    public interface ITransfer
    {
        
    }
}