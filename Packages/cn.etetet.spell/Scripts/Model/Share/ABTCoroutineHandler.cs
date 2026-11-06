using System;

namespace ET
{
    /// <summary>
    /// 异步协程行为树节点处理器基类，专用于 EffectServerBuffTick 树下的异步 AI 动作节点（如移动、巡逻、等待等）。
    /// 通过 BuffTickComponent 记录当前运行的节点 ID 与 ETCancellationToken，实现行为树状态切换时自动打断旧协程。
    /// </summary>
    [BTHandler]
    public abstract class ABTCoroutineHandler<Node>: HandlerObject, IBTHandler where Node: BTCoroutine
    {
        public virtual Type GetNodeType()
        {
            return typeof (Node);
        }
        
        /// <summary>
        /// 同步入口：若当前节点已在运行且未发生热重载则直接返回，否则取消上一个异步协程并启动当前节点的异步协程。
        /// </summary>
        public virtual int Handle(BTNode btNode, BTEnv env)
        {
            Node node = (Node)btNode;
            Buff buff = env.GetEntity<Buff>(node.Buff);

#if UNITY_EDITOR
            env.AddPath(node.Id);
#endif

            BuffTickComponent buffTickComponent = buff.GetComponent<BuffTickComponent>();
            if (buffTickComponent == null)
            {
                throw new Exception("是否EffectServerBuffTick节点没有设置IsAI");
            }

#if UNITY_EDITOR
                EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env });
#endif
            
            // 如果热重载了，那么hashcode会变化，不等于的话就会往下走取消当前协程
            if (buffTickComponent.Current == node.Id && buffTickComponent.HashCode == this.GetHashCode())
            {
                return 0;
            }

            // 取消上一个协程
            buffTickComponent.CancellationToken?.Cancel();
            buffTickComponent.CancellationToken = null;
            buffTickComponent.CancellationToken = new ETCancellationToken();

            buffTickComponent.Current = node.Id;
            buffTickComponent.HashCode = this.GetHashCode();

            RunWithChildrenAsync(buffTickComponent, node, env).Coroutine(buffTickComponent.CancellationToken);
            return 0;
        }

        /// <summary>
        /// 子类需实现的异步执行逻辑。
        /// </summary>
        protected abstract ETTask RunAsync(Node node, BTEnv env);

        /// <summary>
        /// 包装异步执行逻辑，在异步任务未取消且存在子节点时，继续驱动其首个子节点执行，并妥善管理 BTEnv 对象池回收。
        /// </summary>
        private async ETTask RunWithChildrenAsync(BuffTickComponent buffTickComponent, Node node, BTEnv env)
        {
            EntityRef<BuffTickComponent> buffTickComponentRef = buffTickComponent;
            ETCancellationToken cancellationToken = null;
            if (node.Children.Count > 0)
            {
                env.IsFromPool = false; // 有协程，不要回收env
                cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            }

            await this.RunAsync(node, env);
            
            if (cancellationToken.IsCancel() || node.Children.Count <= 0)
            {
                buffTickComponent = buffTickComponentRef;
                if (buffTickComponent != null)
                {
                    // 这里需要重置，否则无法重新执行这个协程
                    buffTickComponent.Current = 0;
                }
                return;
            }

            // 这里可以继续让env回收
            env.IsFromPool = true;
            using BTEnv _ = env;

            BTDispatcher.Instance.Handle(node.Children[0], env);

#if UNITY_EDITOR
            env.RunPath.Add(node.Id);
            EventSystem.Instance.Publish(env.Scene.Entity, new BTRunTreeEvent() { Root = null, Env = env});
#endif
        }
    }
}
