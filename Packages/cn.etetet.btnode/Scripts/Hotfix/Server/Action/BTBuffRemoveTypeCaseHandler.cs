namespace ET.Server
{
    /// <summary>
    /// 服务端判断 Buff 移除原因条件节点处理器：用于 EffectServerBuffRemove 树下，仅当 Buff 移除原因匹配指定 BuffFlags 时才返回 0（成功）。
    /// </summary>
    public class BTBuffRemoveTypeCaseHandler: ABTHandler<BTBuffRemoveTypeCase>
    {
        protected override int Run(BTBuffRemoveTypeCase node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff.GetComponent<BuffRemoveTypeComponent>().BuffRemoveType != node.BuffRemoveType)
            {
                return -1;
            }
            return 0;
        }
    }
}