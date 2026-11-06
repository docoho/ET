using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Test
{
    /// <summary>
    /// 测试用例执行调度辅助类，支持单条执行、串行批量执行以及分批并发（Parallel + Exclusive）执行。
    /// </summary>
    public static class TestRunHelper
    {
        /// <summary>
        /// 运行单个测试用例并捕获耗时、返回值与异常，组装为 TestCaseResult
        /// </summary>
        public static async ETTask<TestCaseResult> RunOne(Fiber fiber, TestArgs options, TestCaseInfo testCaseInfo)
        {
            TimeInfo timeInfo = fiber.GetSingleton<TimeInfo>();
            long beginTime = timeInfo.ServerNow();
            Log.Console($"--------------------------------------------------------------------");
            Log.Console($"\u001b[34m{testCaseInfo.Name} start\u001b[0m");

            try
            {
                int error = await testCaseInfo.Handler.Handle(new TestContext { Fiber = fiber, Args = options });
                long elapsedMilliseconds = timeInfo.ServerNow() - beginTime;
                if (error == 0)
                {
                    Log.Console($"\u001b[32m{testCaseInfo.Name} success\u001b[0m");
                }
                else
                {
                    Log.Console($"\u001b[31m{testCaseInfo.Name} fail! ret: {error}\u001b[0m");
                }

                return new TestCaseResult(testCaseInfo.Name, error, elapsedMilliseconds, null);
            }
            catch (Exception e)
            {
                long elapsedMilliseconds = timeInfo.ServerNow() - beginTime;
                Log.Console($"\u001b[31m{testCaseInfo.Name} fail!\n{e}\u001b[0m");
                return new TestCaseResult(testCaseInfo.Name, 1, elapsedMilliseconds, e);
            }
        }

        public static async ETTask<List<TestCaseResult>> Run(Fiber fiber, TestArgs options, List<TestCaseInfo> testCases)
        {
            return options.Parallel
                    ? await RunGrouped(fiber, options, testCases)
                    : await RunSerial(fiber, options, testCases);
        }

        private static async ETTask<List<TestCaseResult>> RunSerial(Fiber fiber, TestArgs options, List<TestCaseInfo> testCases)
        {
            List<TestCaseResult> results = new();
            foreach (TestCaseInfo testCaseInfo in testCases)
            {
                results.Add(await RunOne(fiber, options, testCaseInfo));
            }

            return results;
        }

        private static async ETTask<List<TestCaseResult>> RunGrouped(Fiber fiber, TestArgs options, List<TestCaseInfo> testCases)
        {
            List<TestCaseResult> results = new();
            List<TestCaseInfo> parallelCases = testCases.Where(t => t.Mode == TestExecutionMode.Parallel).ToList();
            List<TestCaseInfo> exclusiveCases = testCases.Where(t => t.Mode == TestExecutionMode.Exclusive).ToList();

            int maxConcurrency = ResolveMaxConcurrency(options.MaxConcurrency, parallelCases.Count);
            for (int index = 0; index < parallelCases.Count; index += maxConcurrency)
            {
                List<ETTask> batchTasks = new();
                foreach (TestCaseInfo testCaseInfo in parallelCases.Skip(index).Take(maxConcurrency))
                {
                    batchTasks.Add(RunAndCollect(fiber, options, testCaseInfo, results));
                }

                await ETTask.WaitAll(batchTasks);
            }

            foreach (TestCaseInfo testCaseInfo in exclusiveCases.OrderBy(t => t.Name))
            {
                results.Add(await RunOne(fiber, options, testCaseInfo));
            }

            return results.OrderBy(t => t.Name).ToList();
        }

        public static int ResolveMaxConcurrency(int requested, int total)
        {
            if (total <= 0)
            {
                return 1;
            }

            if (requested > 0)
            {
                return Math.Min(requested, total);
            }

            return Math.Min(Math.Max(Environment.ProcessorCount, 1), total);
        }

        private static async ETTask RunAndCollect(Fiber fiber, TestArgs options, TestCaseInfo testCaseInfo, List<TestCaseResult> results)
        {
            TestCaseResult result = await RunOne(fiber, options, testCaseInfo);
            results.Add(result);
        }
    }
}
