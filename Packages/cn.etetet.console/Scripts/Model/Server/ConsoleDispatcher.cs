using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端控制台命令分发器全局单例。
    /// <para>在初始化时扫描所有标记了 <see cref="ConsoleHandlerAttribute"/> 的类型，建立命令模式字符串（Mode）到 <see cref="IConsoleHandler"/> 实例的映射表。</para>
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class ConsoleDispatcher: Singleton<ConsoleDispatcher>, ISingletonAwake
    {
        /// <summary>命令模式名称到对应控制台处理器的映射字典。</summary>
        private readonly Dictionary<string, IConsoleHandler> handlers = new();
        
        /// <summary>
        /// 扫描并实例化所有标记了 <see cref="ConsoleHandlerAttribute"/> 的处理器。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (ConsoleHandlerAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(ConsoleHandlerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                ConsoleHandlerAttribute consoleHandlerAttribute = (ConsoleHandlerAttribute)attrs[0];

                object obj = Activator.CreateInstance(type);

                IConsoleHandler iConsoleHandler = obj as IConsoleHandler;
                if (iConsoleHandler == null)
                {
                    throw new Exception($"ConsoleHandler handler not inherit IConsoleHandler class: {obj.GetType().FullName}");
                }
                this.handlers.Add(consoleHandlerAttribute.Mode, iConsoleHandler);
            }
        }

        public IConsoleHandler Get(string key)
        {
            return this.handlers[key];
        }
    }
}
