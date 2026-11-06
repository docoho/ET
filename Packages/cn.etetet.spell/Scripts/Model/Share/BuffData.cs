namespace ET
{
    // buff上下文,不会被dispose
    [ComponentOf(typeof(Buff))]
    public class BuffData: Entity, IAwake
    {
        /// <summary>
        /// 父 Buff 的上下文引用，使子 Buff 能够向上追溯主技能选择的目标（如 SpellTargetComponent）。
        /// </summary>
        public EntityRef<BuffData> ParentData;
    }
}