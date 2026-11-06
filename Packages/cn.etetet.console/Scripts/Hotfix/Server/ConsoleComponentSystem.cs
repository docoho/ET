using System;
using System.Threading;
using System.Threading.Tasks;

namespace ET.Server
{
    /// <summary>
    /// 服务端控制台交互组件（<see cref="ConsoleComponent"/>）的业务系统类。
    /// <para>在后台线程循环读取标准输入（<c>Console.In.ReadLine()</c>），支持单次命令执行与基于 <see cref="ModeContex"/> 的多轮交互式子模式（输入 <c>exit</c> 退出当前子模式），并将命令分发给对应的 <see cref="IConsoleHandler"/>。</para>
    /// </summary>
    [EntitySystemOf(typeof(ConsoleComponent))]
    public static partial class ConsoleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ConsoleComponent self)
        {
            self.Start().Coroutine();
        }
        
        [EntitySystem]
        private static void Destroy(this ConsoleComponent self)
        {
        }


        /// <summary>
        /// 启动控制台标准输入异步读取循环，解析命令行并路由给 <see cref="ConsoleDispatcher"/>。
        /// </summary>
        private static async ETTask Start(this ConsoleComponent self)
        {
            self.CancellationTokenSource = new CancellationTokenSource();
            EntityRef<ConsoleComponent> selfRef = self;
            while (true)
            {
                try
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    ModeContex modeContex = self.GetComponent<ModeContex>();
                    EntityRef<ModeContex> modeContexRef = modeContex;
                    string prefix = $"{modeContex?.Mode ?? ""}> ";
                    string line = await Task.Factory.StartNew(() =>
                    {
                        Console.Write(prefix);
                        string s = Console.In.ReadLine();
                        return s;
                    }, self.CancellationTokenSource.Token);

                    self = selfRef;

                    // 检测到EOF时退出循环
                    if (line == null)
                    {
                        break;
                    }

                    line = line.Trim();

                    
                    switch (line)
                    {
                        case "":
                            break;
                        case "exit":
                            self.RemoveComponent<ModeContex>();
                            break;
                        default:
                            {
                                modeContex = modeContexRef;
                                string[] lines = line.Split(" ");
                                string mode = modeContex == null ? lines[0] : modeContex.Mode;

                                IConsoleHandler iConsoleHandler = ConsoleDispatcher.Instance.Get(mode);
                                if (modeContex == null)
                                {
                                    modeContex = self.AddComponent<ModeContex>();
                                    modeContex.Mode = mode;
                                }

                                await iConsoleHandler.Run(self.Fiber(), modeContex, line);
                                
                                self = selfRef;
                                break;
                            }
                    }


                }
                catch (Exception e)
                {
                    Log.Console(e.ToString());
                }
            }
        }
    }
}