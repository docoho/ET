using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// 网络与 Actor 消息收发调试日志打印单例（仅在 <c>DEBUG</c> 宏开启时生效，Release 编译自动剔除调用）。
    /// </summary>
    [AllowInstance]
    public class LogMsg: Singleton<LogMsg>, ISingletonAwake
    {
        /// <summary>
        /// 忽略打印的高频消息类型集合（如移动同步、心跳包、帧同步输入包等，可通过 <see cref="AddIgnore"/> 加入黑名单以免刷屏）。
        /// </summary>
        private readonly HashSet<Type> ignore = new();

        public void Awake()
        {
        }

        /// <summary>
        /// 将指定的消息类型加入日志忽略集合，不再打印其收发日志。
        /// </summary>
        public void AddIgnore(Type type)
        {
            this.ignore.Add(type);
        }

        /// <summary>
        /// 在 DEBUG 模式下记录消息发送日志。
        /// </summary>
        [Conditional("DEBUG")]
        public void Send(Fiber fiber, object msg)
        {
            if (msg is IMessageWrapper messageWrapper)
            {
                msg = messageWrapper.GetMessageObject();
            }

            Type type = msg.GetType();
            if (this.ignore.Contains(type))
            {
                return;
            }
            fiber.Log.Debug($"Send: {msg}");
        }
        
        /// <summary>
        /// 在 DEBUG 模式下记录消息接收日志。
        /// </summary>
        [Conditional("DEBUG")]
        public void Recv(Fiber fiber, object msg)
        {
            if (msg is IMessageWrapper messageWrapper)
            {
                msg = messageWrapper.GetMessageObject();
            }
            
            Type type = msg.GetType();
            if (this.ignore.Contains(type))
            {
                return;
            }
            fiber.Log.Debug($"Recv: {msg}");
        }
    }
}
