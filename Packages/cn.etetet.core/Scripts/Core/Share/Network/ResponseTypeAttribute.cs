using System;

namespace ET
{
    /// <summary>
    /// 标记在 <see cref="IRequest"/> 消息类上的特性（通常由 Proto2CS 根据 proto 文件中的 <c>//ResponseType XXX</c> 注释自动生成），
    /// 用于指明该请求对应的 <see cref="IResponse"/> 响应类名。
    /// </summary>
    public class ResponseTypeAttribute: BaseAttribute
    {
        /// <summary>
        /// 对应的响应消息类名（不含命名空间，如 <c>"G2C_Login"</c>）。
        /// </summary>
        public string Type { get; }

        public ResponseTypeAttribute(string type)
        {
            this.Type = type;
        }
    }
}