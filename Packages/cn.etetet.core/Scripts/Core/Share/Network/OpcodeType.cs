using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 协议号（Opcode）与消息类型（<see cref="Type"/>）、RPC 请求与响应类型映射关系的全局只读单例。
    /// <remarks>
    /// 在进程启动时扫描所有带有 <see cref="MessageAttribute"/> 的协议类建立双向映射表 <see cref="typeOpcode"/>，
    /// 并扫描所有 <see cref="IRequest"/> 类型上的 <see cref="ResponseTypeAttribute"/> 建立请求-响应配对表 <see cref="requestResponse"/>。
    /// 由于初始化后只读不变，支持跨 Fiber / 多线程并发安全读取。
    /// </remarks>
    /// </summary>
    [AllowInstance]
    public class OpcodeType: Singleton<OpcodeType>, ISingletonAwake
    {
        // 初始化后不变，所以主线程，网络线程都可以读
        private readonly DoubleMap<Type, ushort> typeOpcode = new();
        
        private readonly Dictionary<Type, Type> requestResponse = new();
        
        /// <summary>
        /// 扫描全程序集带有 <see cref="MessageAttribute"/> 的消息类，初始化 Opcode 双向映射与 Request-Response 类型配对。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageAttribute));
            foreach (Type type in types)
            {
                object[] att = type.GetCustomAttributes(typeof (MessageAttribute), false);
                if (att.Length == 0)
                {
                    continue;
                }

                MessageAttribute messageAttribute = att[0] as MessageAttribute;
                if (messageAttribute == null)
                {
                    continue;
                }

                ushort opcode = messageAttribute.Opcode;
                if (opcode != 0)
                {
                    this.typeOpcode.Add(type, opcode);    
                }

                // 检查request response
                if (typeof (IRequest).IsAssignableFrom(type))
                {
                    if (typeof (ILocationMessage).IsAssignableFrom(type))
                    {
                        this.requestResponse.Add(type, typeof (MessageResponse));
                        continue;
                    }

                    var attrs = type.GetCustomAttributes(typeof (ResponseTypeAttribute), false);
                    if (attrs.Length == 0)
                    {
                        Log.Error($"not found responseType: {type}");
                        continue;
                    }

                    ResponseTypeAttribute responseTypeAttribute = attrs[0] as ResponseTypeAttribute;
                    this.requestResponse.Add(type, CodeTypes.Instance.GetType($"ET.{responseTypeAttribute.Type}"));
                }
            }
        }
        
        /// <summary>
        /// 根据消息 <see cref="Type"/> 查询其对应的 16 位协议号（Opcode）。
        /// </summary>
        public ushort GetOpcode(Type type)
        {
            ushort opcode = this.typeOpcode.GetValueByKey(type);
            if (opcode == 0)
            {
                throw new Exception($"OpcodeType not found opcode: {type.FullName}");
            }
            return opcode;
        }

        /// <summary>
        /// 根据 16 位协议号（Opcode）反查对应的消息 <see cref="Type"/>。
        /// </summary>
        public Type GetType(ushort opcode)
        {
            Type type = this.typeOpcode.GetKeyByValue(opcode);
            if (type == null)
            {
                throw new Exception($"OpcodeType not found type: {opcode}");
            }
            return type;
        }

        /// <summary>
        /// 根据 RPC 请求类型（<see cref="IRequest"/>）查询其配对的响应类型（<see cref="IResponse"/>），用于在 RPC 超时或异常时构造默认错误响应。
        /// </summary>
        public Type GetResponseType(Type request)
        {
            if (!this.requestResponse.TryGetValue(request, out Type response))
            {
                throw new Exception($"not found response type, request type: {request.FullName}");
            }

            return response;
        }
    }
}
