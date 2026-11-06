using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 控制台移除机器人命令（RemoveRobot）的命令行参数解析模型
    /// </summary>
    public class RemoveRobotArgs: Object
    {
        /// <summary>
        /// 要移除的机器人账号名
        /// </summary>
        [Option("Account", Required = false, Default = 1)]
        public string Account { get; set; }
    }
}