using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// 全局事件发布（Publish）与回调调用（Invoke）分发中心单例。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— <c>Publish</c>（事件）与 <c>Invoke</c>（回调）的核心区别】
    /// 1. <b><see cref="Publish{S,T}"/> / <see cref="PublishAsync{S,T}"/>（一对多，跨模块解耦）</b>：
    ///    - 用于<b>不同模块之间</b>的通知。例如：玩家升到了 10 级，等级模块抛出一个 <c>UnitLevelUp</c> 事件结构体；
    ///    - 此时成就模块、任务模块、UI 模块、技能模块各自编写一个 <c>[Event(SceneType.XXX)] class OnLevelUp_XXX : AEvent&lt;Scene, UnitLevelUp&gt;</c> 来订阅它。
    ///    - 等级模块完全不需要知道有谁订阅了这个事件，哪怕有 0 个或 10 个订阅者都可以正常运行！
    /// 2. <b><see cref="Invoke{A}(long,A)"/> / <see cref="Invoke{A,T}(long,A)"/>（一对一，同模块跨层或按 ID 多态分发）</b>：
    ///    - 类似于<b>C# 的委托/虚函数调用</b>，<b>必须有且仅有一个对应的 <see cref="AInvokeHandler{A}"/> 处理器</b>（若找不到会直接抛异常，除非用 <c>TryInvoke</c>）。
    ///    - 什么时候用 <c>Invoke</c>？
    ///      a) <b>跨程序集向下调用</b>：比如底层的 <c>ET.Core</c>（如 <see cref="TimerComponent"/> 定时器触发时）需要调用上层 <c>ET.Hotfix</c> 里的具体定时逻辑，但底层不能直接引用上层，于是通过 <c>Invoke(timerClassId, args)</c> 回调上层！
    ///      b) <b>双端差异化实现</b>：比如同一个配置加载请求，在客户端用 YooAssets 加载，在服务端直接读文件，通过注册不同的 <c>AInvokeHandler</c> 实现多态。
    ///    - <b>切记</b>：如果可以直接用普通静态函数调用，绝对不要滥用 <c>Invoke</c>，否则会降低代码可读性！
    /// </remarks>
    [CodeProcess]
    [AllowInstance]
    public class EventSystem: Singleton<EventSystem>, ISingletonAwake
    {
        /// <summary>
        /// 内部封装：单条事件订阅处理器及其绑定的目标 <see cref="SceneType"/>。
        /// </summary>
        private class EventInfo
        {
            public IEvent IEvent { get; }
            
            public int SceneType {get; }

            public EventInfo(IEvent iEvent, int sceneType)
            {
                this.IEvent = iEvent;
                this.SceneType = sceneType;
            }
        }
        
        /// <summary>存储事件参数结构体 Type -> 所有订阅该事件的处理器列表（一对多）</summary>
        private readonly Dictionary<Type, List<EventInfo>> allEvents = new();
        
        /// <summary>存储 Invoke 参数结构体 Type -> (区分长整型 TypeId -> 唯一处理器实例) 的二级字典（一对一）</summary>
        private readonly Dictionary<Type, Dictionary<long, object>> allInvokers = new();
        
        /// <summary>
        /// 启动时从 <see cref="CodeTypes"/> 扫描所有标注了 <see cref="EventAttribute"/> 和 <see cref="InvokeAttribute"/> 的处理器类并完成实例化注册。
        /// </summary>
        public void Awake()
        {
            CodeTypes codeTypes = CodeTypes.Instance;
            foreach (Type type in codeTypes.GetTypes(typeof (EventAttribute)))
            {
                IEvent obj = Activator.CreateInstance(type) as IEvent;
                if (obj == null)
                {
                    throw new Exception($"type not is AEvent: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);
                foreach (object attr in attrs)
                {
                    EventAttribute eventAttribute = attr as EventAttribute;

                    Type eventType = obj.Type;

                    EventInfo eventInfo = new(obj, eventAttribute.SceneType);

                    if (!this.allEvents.ContainsKey(eventType))
                    {
                        this.allEvents.Add(eventType, new List<EventInfo>());
                    }
                    this.allEvents[eventType].Add(eventInfo);
                }
            }

            foreach (Type type in codeTypes.GetTypes(typeof (InvokeAttribute)))
            {
                object obj = Activator.CreateInstance(type);
                IInvoke iInvoke = obj as IInvoke;
                if (iInvoke == null)
                {
                    throw new Exception($"type not is callback: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(InvokeAttribute), false);
                foreach (object attr in attrs)
                {
                    if (!this.allInvokers.TryGetValue(iInvoke.Type, out var dict))
                    {
                        dict = new Dictionary<long, object>();
                        this.allInvokers.Add(iInvoke.Type, dict);
                    }
                    
                    InvokeAttribute invokeAttribute = attr as InvokeAttribute;
                    
                    try
                    {
                        dict.Add(invokeAttribute.Type, obj);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"action type duplicate: {iInvoke.Type.Name} {invokeAttribute.Type}", e);
                    }
                }
            }
        }
        
        /// <summary>
        /// 异步发布事件并等待所有匹配 <paramref name="scene"/> 类型的订阅者（<see cref="AEvent{S,A}"/>）全部执行完毕（内部通过 <see cref="ETTask.WaitAll(List{ETTask})"/> 并发等待）。
        /// </summary>
        public async ETTask PublishAsync<S, T>(S scene, T a) where S: class, IScene where T : struct
        {
            List<EventInfo> iEvents;
            if (!this.allEvents.TryGetValue(typeof(T), out iEvents))
            {
                return;
            }

            using ListComponent<ETTask> list = ListComponent<ETTask>.Create();

            int sceneType = scene.SceneType;
            foreach (EventInfo eventInfo in iEvents)
            {
                if (!SceneTypeSingleton.IsSame(sceneType, eventInfo.SceneType))
                {
                    continue;
                }
                    
                if (!(eventInfo.IEvent is AEvent<S, T> aEvent))
                {
                    Log.Error($"event error: {eventInfo.IEvent.GetType().FullName}");
                    continue;
                }

                list.Add(aEvent.Handle(scene, a));
            }

            try
            {
                await ETTask.WaitAll(list);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// 非阻塞发布事件：触发所有匹配 <paramref name="scene"/> 类型的订阅者（以 <c>.Coroutine()</c> 方式各自启动，不等待其异步完成）。
        /// </summary>
        public void Publish<S, T>(S scene, T a) where S: class, IScene where T : struct
        {
            List<EventInfo> iEvents;
            if (!this.allEvents.TryGetValue(typeof (T), out iEvents))
            {
                return;
            }

            int sceneType = scene.SceneType;
            foreach (EventInfo eventInfo in iEvents)
            {
                if (!SceneTypeSingleton.IsSame(sceneType, eventInfo.SceneType))
                {
                    continue;
                }

                
                if (!(eventInfo.IEvent is AEvent<S, T> aEvent))
                {
                    Log.Error($"event error: {eventInfo.IEvent.GetType().FullName}");
                    continue;
                }
                
                aEvent.Handle(scene, a).Coroutine();
            }
        }
        
        /// <summary>
        /// 根据参数结构体类型 <typeparamref name="A"/> 与子类型编号 <paramref name="type"/> 查找已注册的 Invoke 处理器。
        /// </summary>
        public Invoker GetInvoker<Invoker, A>(long type) where Invoker : class, IInvoke where A: struct
        {
            if (!this.allInvokers.TryGetValue(typeof(A), out Dictionary<long, object> invokeHandlers))
            {
                return null;
            }
            if (!invokeHandlers.TryGetValue(type, out object invokeHandler))
            {
                return null;
            }

            if (invokeHandler is not Invoker aInvokeHandler)
            {
                return null;
            }
            return aInvokeHandler;
        }

        /// <summary>
        /// 获取指定 Invoke 参数结构体 <typeparamref name="A"/> 下所有已注册的子类型编号（<c>type</c>）列表。
        /// </summary>
        public List<long> GetAllInvokerTypes<A>() where A : struct
        {
            if (!this.allInvokers.TryGetValue(typeof(A), out Dictionary<long, object> invokeHandlers))
            {
                return new List<long>();
            }
            return invokeHandlers.Keys.ToList();
        }
        
        /// <summary>
        /// 按子类型编号 <paramref name="type"/> 调用无返回值的 <see cref="AInvokeHandler{A}"/>（若未找到对应处理器则抛出异常）。
        /// </summary>
        // Invoke跟Publish的区别(特别注意)
        // Invoke类似函数，必须有被调用方，否则异常，调用者跟被调用者属于同一模块，比如MoveComponent中的Timer计时器，调用跟被调用的代码均属于移动模块
        // 既然Invoke跟函数一样，那么为什么不使用函数呢? 因为有时候不方便直接调用，比如Config加载，在客户端跟服务端加载方式不一样。比如TimerComponent需要根据Id分发
        // 注意，不要把Invoke当函数使用，这样会造成代码可读性降低，能用函数不要用Invoke
        // publish是事件，抛出去可以没人订阅，调用者跟被调用者属于两个模块，比如任务系统需要知道道具使用的信息，则订阅道具使用事件
        public void Invoke<A>(long type, A args) where A: struct
        {
            var aInvokeHandler = this.GetInvoker<AInvokeHandler<A>, A>(type);
            if (aInvokeHandler == null)
            {
                throw new Exception($"Invoke error, not AInvokeHandler: {type} {typeof(A).FullName}");
            }
            aInvokeHandler.Handle(args);
        }
        
        /// <summary>
        /// 按子类型编号 <paramref name="type"/> 调用带返回值 <typeparamref name="T"/> 的 <see cref="AInvokeHandler{A,T}"/>（若未找到对应处理器则抛出异常）。
        /// </summary>
        public T Invoke<A, T>(long type, A args) where A: struct
        {
            var aInvokeHandler = this.GetInvoker<AInvokeHandler<A, T>, A>(type);
            if (aInvokeHandler == null)
            {
                throw new Exception($"Invoke error, not AInvokeHandler: {type} {typeof(A).FullName}");
            }
            return aInvokeHandler.Handle(args);
        }
        
        /// <summary>
        /// 调用默认 <c>type = 0</c> 的无返回值 <see cref="AInvokeHandler{A}"/>。
        /// </summary>
        public void Invoke<A>(A args) where A: struct
        {
            Invoke(0, args);
        }
        
        /// <summary>
        /// 调用默认 <c>type = 0</c> 的带返回值 <typeparamref name="T"/> 的 <see cref="AInvokeHandler{A,T}"/>。
        /// </summary>
        public T Invoke<A, T>(A args) where A: struct
        {
            return Invoke<A, T>(0, args);
        }
        
        /// <summary>
        /// 尝试按 <paramref name="type"/> 调用无返回值的 <see cref="AInvokeHandler{A}"/>（若未注册处理器则静默忽略，不抛异常）。
        /// </summary>
        // Invoke跟Publish的区别(特别注意)
        // Invoke类似函数，必须有被调用方，否则异常，调用者跟被调用者属于同一模块，比如MoveComponent中的Timer计时器，调用跟被调用的代码均属于移动模块
        // 既然Invoke跟函数一样，那么为什么不使用函数呢? 因为有时候不方便直接调用，比如Config加载，在客户端跟服务端加载方式不一样。比如TimerComponent需要根据Id分发
        // 注意，不要把Invoke当函数使用，这样会造成代码可读性降低，能用函数不要用Invoke
        // publish是事件，抛出去可以没人订阅，调用者跟被调用者属于两个模块，比如任务系统需要知道道具使用的信息，则订阅道具使用事件
        public void TryInvoke<A>(long type, A args) where A: struct
        {
            var aInvokeHandler = this.GetInvoker<AInvokeHandler<A>, A>(type);
            if (aInvokeHandler == null)
            {
                return;
            }
            aInvokeHandler.Handle(args);
        }
        
        /// <summary>
        /// 尝试按 <paramref name="type"/> 调用带返回值 <typeparamref name="T"/> 的 <see cref="AInvokeHandler{A,T}"/>（若未注册则返回 <c>default</c>）。
        /// </summary>
        public T TryInvoke<A, T>(long type, A args) where A: struct
        {
            var aInvokeHandler = this.GetInvoker<AInvokeHandler<A, T>, A>(type);
            if (aInvokeHandler == null)
            {
                return default;
            }
            return aInvokeHandler.Handle(args);
        }
        
        /// <summary>
        /// 尝试调用默认 <c>type = 0</c> 的无返回值 <see cref="AInvokeHandler{A}"/>。
        /// </summary>
        public void TryInvoke<A>(A args) where A: struct
        {
            TryInvoke(0, args);
        }
        
        /// <summary>
        /// 尝试调用默认 <c>type = 0</c> 的带返回值 <typeparamref name="T"/> 的 <see cref="AInvokeHandler{A,T}"/>。
        /// </summary>
        public T TryInvoke<A, T>(A args) where A: struct
        {
            return TryInvoke<A, T>(0, args);
        }
    }
}
