namespace ET
{
    /// <summary>
    /// BuffData 实体系统，在创建时自动将自身绑定到父 Buff 的 BuffData 引用上。
    /// </summary>
    [EntitySystemOf(typeof(BuffData))]
    public static partial class BuffDataSystem
    {
        [EntitySystem]
        private static void Awake(this BuffData self)
        {
            Buff buff = self.GetParent<Buff>();
            buff.BuffData = self;
        }
    }
}