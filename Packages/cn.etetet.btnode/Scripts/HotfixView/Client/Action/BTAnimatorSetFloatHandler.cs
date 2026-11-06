namespace ET.Client
{
    /// <summary>
    /// 客户端设置角色动画状态机（Animator）Float 参数的节点处理器。
    /// </summary>
    public class BTAnimatorSetFloatHandler: ABTHandler<BTAnimatorSetFloat>
    {
        protected override int Run(BTAnimatorSetFloat node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetFloat(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}