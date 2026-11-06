namespace ET
{
    /// <summary>
    /// 网络协议消息标记特性（通常由 Proto2CS 工具自动生成在每个协议类上），用于将消息类与 16 位无符号协议号（<see cref="Opcode"/>）绑定。
    /// </summary>
    public class MessageAttribute: BaseAttribute
    {
        /// <summary>
        /// 消息对应的唯一 16 位协议号（0 ~ 65535）。
        /// </summary>
        public ushort Opcode
        {
            get;
        }

        public MessageAttribute(ushort opcode = 0)
        {
            this.Opcode = opcode;
        }
    }
}