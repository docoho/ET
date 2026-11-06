using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 判断目标 Unit 是否位于施法者前方（朝向向量与目标方向向量点积 >= 0）的条件节点处理器。
    /// </summary>
    public class BTTargetInFrontOfCasterHandler: ABTHandler<BTTargetInFrontOfCaster>
    {
        protected override int Run(BTTargetInFrontOfCaster node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            float v = math.dot(caster.Forward, target.Position - caster.Position);
            if (v < 0)
            {
                return TextConstDefine.SpellCast_TargetNotInFrontOfCaster;
            }
            return 0;
        }
    }
}