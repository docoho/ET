namespace ET.Server
{
    /// <summary>
    /// 服务端控制台命令模式扩展定义（数据库管理指令）。
    /// </summary>
    public static partial class ConsoleMode
    {
        /// <summary>
        /// 创建数据库模式指令。
        /// </summary>
        public const string CreateDB = "CreateDB";

        /// <summary>
        /// 删除数据库模式指令。
        /// </summary>
        public const string DropDB = "DropDB";

        /// <summary>
        /// 更新数据库模式指令。
        /// </summary>
        public const string UpdateDB = "UpdateDB";
    }
}