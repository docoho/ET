namespace ET.Client
{
    /// <summary>
    /// 客户端触发角色动画状态机（Animator）Trigger 参数的节点处理器。
    /// </summary>
    public class BTAnimatorSetTriggerHandler: ABTHandler<BTAnimatorSetTrigger>
    {
        protected override int Run(BTAnimatorSetTrigger node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
            return 0;
        }
    }
}