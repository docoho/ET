namespace ET.Server
{
    /// <summary>
    /// 记录当前 <c>RouterManager</c> 对外 HTTP 监听地址（<c>IP:Port</c>）的组件。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class RouterManagerAddressComponent : Entity, IAwake
    {
        /// <summary><c>RouterManager</c> 的 HTTP 访问地址字符串。</summary>
        public string Address { get; set; }
    }
}
