using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端技能目标选择器处理器特性，用于自动扫描并注册到 TargetSelectDispatcher。
    /// </summary>
    public class TargetSelectHandlerAttribute: BaseAttribute
    {
    }
    
    /// <summary>
    /// 客户端技能目标选择器处理器接口。
    /// </summary>
    public interface ITargetSelectHandler
    {
        /// <summary>
        /// 执行客户端目标选择交互（例如展示技能指示器并等待玩家确认或取消）。
        /// </summary>
        ETTask<int> Handle(TargetSelector node, EntityRef<Unit> unit, SpellConfig spellConfig);
        /// <summary>
        /// 获取该处理器对应的 TargetSelector 节点类型。
        /// </summary>
        Type GetNodeType();
    }
    
    /// <summary>
    /// 客户端技能目标选择器泛型抽象基类，提供强类型的 Node 参数转换。
    /// </summary>
    [TargetSelectHandler]
    public abstract class TargetSelectHandler<Node>: HandlerObject, ITargetSelectHandler where Node : TargetSelector
    {
        protected abstract ETTask<int> Run(Node node, EntityRef<Unit> unit, SpellConfig spellConfig);

        public async ETTask<int> Handle(TargetSelector node, EntityRef<Unit> unit, SpellConfig spellConfig)
        {
            if (node is not Node c)
            {
                throw new Exception($"类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }

            return await this.Run(c, unit, spellConfig);
        }

        public Type GetNodeType()
        {
            return typeof (Node);
        }
    }
    
    /// <summary>
    /// 分发组件（客户端技能目标选择分发器单例，根据 TargetSelector 的具体类型或基类分发到对应的交互处理器）
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class TargetSelectDispatcher: Singleton<TargetSelectDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, ITargetSelectHandler> handlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (TargetSelectHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            ITargetSelectHandler targetSelectHandler = obj as ITargetSelectHandler;
            if (targetSelectHandler == null)
            {
                throw new Exception($"target select handler not inherit ITargetSelectHandler abstract class: {obj.GetType().FullName}");
            }

            Type nodeType = targetSelectHandler.GetNodeType();
            this.handlers.TryAdd(nodeType, targetSelectHandler);
        }
        
        
        /// <summary>
        /// 根据传入的 TargetSelector 节点类型向上回溯继承链，查找并调用对应的客户端目标选择处理器。
        /// </summary>
        public ETTask<int> Handle(TargetSelector node, EntityRef<Unit> unit, SpellConfig spellConfig)
        {
            Type handlerType = node.GetType();
            while (handlerType != null)
            {
                if (this.handlers.TryGetValue(handlerType, out ITargetSelectHandler targetSelectHandler))
                {
                    return targetSelectHandler.Handle(node, unit, spellConfig);
                }

                handlerType = handlerType.BaseType;
            }

            ETTask<int> task = ETTask<int>.Create();
            task.SetResult(0);
            return task;
        }
    }
}
