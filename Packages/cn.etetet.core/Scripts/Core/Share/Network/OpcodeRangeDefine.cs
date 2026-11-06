namespace ET
{
    /// <summary>
    /// 网络协议号（Opcode）分段范围常量定义。
    /// </summary>
    public static class OpcodeRangeDefine
    {
        /// <summary>
        /// 外网协议最小 Opcode。
        /// </summary>
        public const ushort OuterMinOpcode = 10001;
        /// <summary>
        /// 外网协议最大 Opcode。
        /// </summary>
        public const ushort OuterMaxOpcode = 20000;

        // 20001-30000 内网pb
        /// <summary>
        /// 内网协议最小 Opcode。
        /// </summary>
        public const ushort InnerMinOpcode = 20001;
        /// <summary>
        /// 内网协议最大 Opcode。
        /// </summary>
        public const ushort InnerMaxOpcode = 40000;
        
        /// <summary>
        /// 常规业务协议的最大 Opcode 界限。
        /// </summary>
        public const ushort MaxOpcode = 60000;
    }
}