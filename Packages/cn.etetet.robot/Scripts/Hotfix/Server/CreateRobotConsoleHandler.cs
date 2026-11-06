using System;
using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 控制台批量创建压测机器人命令（<see cref="ConsoleMode.CreateRobot"/>）处理器
    /// </summary>
    [ConsoleHandler(ConsoleMode.CreateRobot)]
    public class CreateRobotConsoleHandler: IConsoleHandler
    {
        /// <summary>
        /// 解析命令行参数 <see cref="CreateRobotArgs"/>，通过 <see cref="RobotManagerComponent"/> 循环创建指定数量的机器人纤程并执行登录
        /// </summary>
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            EntityRef<ModeContex> contexRef = contex;
            try
            {
                switch (content)
                {
                    case ConsoleMode.CreateRobot:
                    {
                        Log.Console("CreateRobot args error!");
                        break;
                    }
                    default:
                    {
                        CreateRobotArgs options = null;
                        Parser.Default.ParseArguments<CreateRobotArgs>(content.Split(' '))
                                .WithNotParsed(error => throw new Exception($"CreateRobotArgs error!"))
                                .WithParsed(o => { options = o; });

                        RobotManagerComponent robotManagerComponent = fiber.Root.GetComponent<RobotManagerComponent>();

                        EntityRef<RobotManagerComponent> robotManagerComponentRef = robotManagerComponent;
                    
                        // 创建机器人
                        for (int i = 0; i < options.Num; ++i)
                        {
                            robotManagerComponent = robotManagerComponentRef;
                            await robotManagerComponent.NewRobot(options.SchedulerType, $"Robot_{i}");
                            Log.Console($"Create Robot_{i}");
                        }
                        break;
                    }
                }
            }
            finally
            {
                contex = contexRef;
                contex.Parent.RemoveComponent<ModeContex>();
            }
        }
    }
}