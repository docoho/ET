namespace ET.Server
{
    /// <summary>
    /// 服务端增减 Buff 层数节点处理器。
    /// </summary>
    public class BTBuffAddStackHandler: ABTHandler<BTBuffAddStack>
    {
        protected override int Run(BTBuffAddStack node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            int stack = buff.Stack + node.Value; 
            BuffHelper.UpdateStack(buff, stack);
            return 0;
        }
    }
}