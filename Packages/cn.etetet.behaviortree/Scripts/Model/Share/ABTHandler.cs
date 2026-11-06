using System;

namespace ET
{
    /// <summary>
    /// 同步行为树节点处理器泛型基类，负责将通用 BTNode 强转为具体 Node 类型，并在编辑器下自动记录执行轨迹。
    /// 约定返回值：0（ErrorCode.ERR_Success）代表成功，非 0 代表失败或具体错误码。
    /// </summary>
    [BTHandler]
    public abstract class ABTHandler<Node>: HandlerObject, IBTHandler where Node : BTNode
    {
        protected abstract int Run(Node node, BTEnv env);

        public virtual int Handle(BTNode node, BTEnv env)
        {
            if (node is not Node c)
            {
                throw new Exception($"AEffectHandler EffectConfig类型转换错误: {node.GetType().FullName} to {typeof (Node).Name}");
            }
#if UNITY_EDITOR
            env.AddPath(node.Id);
#endif
            return this.Run(c, env);
        }

        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
    }
}