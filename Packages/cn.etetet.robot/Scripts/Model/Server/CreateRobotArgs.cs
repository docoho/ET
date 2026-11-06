using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 控制台创建机器人命令（CreateRobot）的命令行参数解析模型
    /// </summary>
    public class CreateRobotArgs: Object
    {
        /// <summary>
        /// 要创建的机器人数量（默认为 1）
        /// </summary>
        [Option("Num", Required = false, Default = 1)]
        public int Num { get; set; }

        /// <summary>
        /// 机器人纤程运行的调度器类型（默认为父线程调度 <see cref="ET.SchedulerType.Parent"/>）
        /// </summary>
        [Option("SchedulerType", Required = false, Default = SchedulerType.Parent)]
        public SchedulerType SchedulerType { get; set; }
    }
}