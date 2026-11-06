namespace ET
{
    /// <summary>
    /// 全局网络消息协议号（Opcode）静态分布类定义。
    /// 具体的协议号常量由 <c>ET.Proto2CS</c> 工具从各包的 <c>.proto</c> 文件生成在同名的 <c>partial class Opcode</c> 中，
    /// 并由 <see cref="UniqueIdAttribute"/> 分析器在编译期静态检查是否存在重复的协议号。
    /// </summary>
    [UniqueId]
    public static partial class Opcode
    {
    }
}