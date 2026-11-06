using System;
using System.Collections.Generic;
using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 控制台移除指定账号机器人命令（<see cref="ConsoleMode.RemoveRobot"/>）处理器
    /// </summary>
    [ConsoleHandler(ConsoleMode.RemoveRobot)]
    public class RemoveRobotConsoleHandler: IConsoleHandler
    {
        /// <summary>
        /// 解析命令行参数 <see cref="RemoveRobotArgs"/>，查找目标机器人纤程并发送 <see cref="Console2Robot_LogoutRequest"/> 使其登出下线
        /// </summary>
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            EntityRef<ModeContex> contexRef = contex;
            try
            {
                switch (content)
                {
                    case ConsoleMode.RemoveRobot:
                    {
                        Log.Console("RemoveRobot args error!");
                        break;
                    }
                    default:
                    {
                        RemoveRobotArgs options = null;
                        Parser.Default.ParseArguments<RemoveRobotArgs>(content.Split(' '))
                                .WithNotParsed(error => throw new Exception($"RemoveRobot error!"))
                                .WithParsed(o => { options = o; });

                        RobotManagerComponent robotManagerComponent = fiber.Root.GetComponent<RobotManagerComponent>();
                        if (robotManagerComponent == null)
                        {
                            Log.Console("RobotManagerComponent not found!");
                            return;
                        }
                        robotManagerComponent.GetRobotActorId(options.Account, out ActorId actorId);
                        ProcessInnerSender processInnerSender = fiber.Root.GetComponent<ProcessInnerSender>();
                        Console2Robot_LogoutRequest request = Console2Robot_LogoutRequest.Create();
                        Console2Robot_LogoutResponse response = await processInnerSender.Call(actorId.FiberInstanceId, request) as Console2Robot_LogoutResponse;
                        Log.Console($"Remove Robot OK: {options.Account}");
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