namespace ET.Client
{
    /// <summary>
    /// 客户端设置角色动画状态机（Animator）Int 参数的节点处理器。
    /// </summary>
    public class BTAnimatorSetIntHandler: ABTHandler<BTAnimatorSetInt>
    {
        protected override int Run(BTAnimatorSetInt node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetInt(node.MotionType.ToString(), node.Value);
            return 0;
        }
    }
}