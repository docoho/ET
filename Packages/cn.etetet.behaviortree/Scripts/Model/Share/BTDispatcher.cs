using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 编辑器下每次运行完一棵行为树时发布的事件，用于在行为树编辑器窗口中实时高亮刚刚走过的节点路径。
    /// </summary>
    public struct BTRunTreeEvent
    {
        public BTRoot Root;
        public BTEnv Env;
    }
    
    /// <summary>
    /// 行为树节点处理器注册特性。
    /// </summary>
    public class BTHandlerAttribute: BaseAttribute
    {
    }
    
    /// <summary>
    /// 行为树节点处理器接口。
    /// </summary>
    public interface IBTHandler
    {
        int Handle(BTNode node, BTEnv env);
        Type GetNodeType();
    }
    
    /// <summary>
    /// Actor消息分发组件（行为树节点执行分发器单例，在启动时收集所有 IBTHandler 并按 BTNode 具体类型进行同步调用分发）
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class BTDispatcher: Singleton<BTDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, IBTHandler> btHandlers = new();

        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (BTHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IBTHandler ibtHandler = obj as IBTHandler;
            if (ibtHandler == null)
            {
                throw new Exception($"bt handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }

            Type nodeType = ibtHandler.GetNodeType();
            this.btHandlers.Add(nodeType, ibtHandler);
        }
        
        
        /// <summary>
        /// 根据传入的 BTNode 类型查找并执行对应的处理器，返回节点执行结果码（0 表示成功）。
        /// </summary>
        public int Handle(BTNode node, BTEnv env)
        {
            if (node == null)
            {
                throw new Exception($"bt node is null");
            }
            if (!this.btHandlers.TryGetValue(node.GetType(), out IBTHandler ibtHandler))
            {
                throw new Exception($"not found bt handler: {node.GetType().FullName}");
            }
            int ret = ibtHandler.Handle(node, env);
            
#if UNITY_EDITOR
            env.AddSnapshot($"Node {node.GetType().Name} {node.Id} return {ret}");
#endif
            return ret;
        }
    }
}
