using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例控制台处理器
    /// 负责解析控制台命令并分发给对应的Test Handler执行
    /// </summary>
    [ConsoleHandler(ConsoleMode.Test)]
    public class TestConsoleHandler : IConsoleHandler
    {
        /// <summary>
        /// 解析 TestArgs 命令行参数，筛选匹配的测试用例并调用 TestRunHelper 批量执行，最后输出汇总报告并以对应退出码退出进程
        /// </summary>
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            EntityRef<ModeContex> modeContexRef = contex;
            try
            {
                TestArgs options = null;
                Parser.Default.ParseArguments<TestArgs>(content.Split(' '))
                        .WithNotParsed(error => throw new Exception($"TestArgs解析错误!"))
                        .WithParsed(o => { options = o; });
                
                List<TestCaseInfo> testCases = TestDispatcher.Instance.Get(options.Name);

                if (testCases.Count == 0)
                {
                    Log.Console("not found test!");
                    Environment.Exit(1);
                }

                TimeInfo timeInfo = fiber.GetSingleton<TimeInfo>();
                long beginTime = timeInfo.ServerNow();
                List<TestCaseResult> results = await TestRunHelper.Run(fiber, options, testCases);
                long elapsedMilliseconds = timeInfo.ServerNow() - beginTime;

                List<TestCaseResult> failedTests = results.Where(t => !t.IsSuccess).ToList();
                int exitCode = failedTests.Count == 0 ? 0 : 1;
                
                Log.Console($"--------------------------------------------------------------------");
                Log.Console("Test Summary:");
                Log.Console($"Total: {testCases.Count}, Passed: {testCases.Count - failedTests.Count}, Failed: {failedTests.Count}, Time: {elapsedMilliseconds}ms");
                if (failedTests.Count > 0)
                {
                    Log.Console("Failed Tests:");
                    foreach (TestCaseResult failedTest in failedTests)
                    {
                        Log.Console($"- {failedTest.Name}");
                    }
                }
                Environment.Exit(exitCode);
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
                Environment.Exit(1);
            }
            finally
            {
                contex = modeContexRef;
                if (contex != null)
                {
                    contex.Parent.RemoveComponent<ModeContex>();    
                }
            }
        }
    }
}
