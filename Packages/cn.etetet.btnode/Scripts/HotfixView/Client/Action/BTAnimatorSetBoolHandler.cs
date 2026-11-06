namespace ET.Client
{
    /// <summary>
    /// 客户端设置角色动画状态机（Animator）Bool 参数的节点处理器。
    /// </summary>
    public class BTAnimatorSetBoolHandler: ABTHandler<BTAnimatorSetBool>
    {
        protected override int Run(BTAnimatorSetBool node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetBool(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}