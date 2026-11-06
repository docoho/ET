using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端添加伪子弹追踪 Buff 节点处理器：根据施法者与目标的距离及飞行速度动态计算 Buff 的过期时间（ExpireTime），在 Buff 超时移除时即可触发命中结算。
    /// </summary>
    public class BTAddFakeBulletBuffHandler: ABTHandler<BTAddFakeBulletBuff>
    {
        protected override int Run(BTAddFakeBulletBuff node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff parentBuff = env.GetEntity<Buff>(node.Buff);

            Buff newBuff = BuffHelper.CreateBuffWithoutInit(target, caster.Id, IdGenerater.Instance.GenerateId(), node.ConfigId, null);
            int flyTime = (int)(math.distance(caster.Position, target.Position) / (node.Speed / 1000f) * 1000);
            newBuff.ExpireTime = newBuff.CreateTime + flyTime;
            
            Buff bulletBuff = BuffHelper.InitBuff(newBuff);

            if (!string.IsNullOrEmpty(node.OutputBuff))
            {
                env.AddEntity(node.OutputBuff, bulletBuff);
            }
            return 0;
        }
    }
}