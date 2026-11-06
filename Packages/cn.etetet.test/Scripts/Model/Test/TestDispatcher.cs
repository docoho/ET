using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// 测试用例全局分发器单例，在启动时扫描所有标记了 [Test] 的处理器并支持按名称正则表达式筛选测试用例。
    /// </summary>
    [AllowInstance]
    public class TestDispatcher: Singleton<TestDispatcher>, ISingletonAwake
    {
        /// <summary>
        /// 按处理器名称字典序排列的测试处理器映射表
        /// </summary>
        private SortedDictionary<string, ITestHandler> dispatcher = new(); 
        
        /// <summary>
        /// 单例初始化，扫描所有 [Test] 特性标记的类型并实例化注册
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (TestAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(TestAttribute), true);
                if (attrs.Length == 0)
                {
                    continue;
                }
                
                object obj = Activator.CreateInstance(type);

                ITestHandler iTestHandler = obj as ITestHandler;
                if (iTestHandler == null)
                {
                    throw new Exception($"handler not inherit ITestHandler class: {obj.GetType().FullName}");
                }

                this.dispatcher.Add(iTestHandler.GetType().Name, iTestHandler);
            }
        }

        /// <summary>
        /// 获取匹配的测试处理器列表
        /// </summary>
        /// <param name="name">处理器名正则表达式，null或空表示匹配所有处理器</param>
        /// <returns>匹配的测试用例描述列表</returns>
        public List<TestCaseInfo> Get(string name)
        {
            List<TestCaseInfo> result = new();
            
            // 创建正则表达式
            System.Text.RegularExpressions.Regex nameRegex = new(name);
            
            // 遍历所有包
            foreach ((string testName, ITestHandler iTestHandler) in this.dispatcher)
            {
                // 如果指定了name正则，检查是否匹配
                if (!nameRegex.IsMatch(testName))
                {
                    continue;
                }
                
                object[] executionAttributes = iTestHandler.GetType().GetCustomAttributes(typeof(TestExecutionAttribute), true);
                TestExecutionMode mode = executionAttributes.Length > 0
                        ? ((TestExecutionAttribute)executionAttributes[0]).Mode
                        : TestExecutionMode.Parallel;

                result.Add(new TestCaseInfo(testName, iTestHandler, mode));
            }

            return result;
        }
    }
}
