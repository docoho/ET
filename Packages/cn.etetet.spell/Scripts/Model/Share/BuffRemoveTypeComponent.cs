namespace ET
{
    /// <summary>
    /// Buff 移除原因记录组件，在 Buff 即将销毁前挂载，供移除效果节点（EffectServerBuffRemove/EffectClientBuffRemove）判断移除原因。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffRemoveTypeComponent: Entity, IAwake
    {
        public BuffFlags BuffRemoveType { get; set; }
    }
}