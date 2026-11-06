namespace ET.Server
{
    /// <summary>
    /// Buff 修改数值记录组件系统（记录该 Buff 对宿主单位所做的数值变更，并在 Buff 移除销毁时自动逆向扣回恢复）。
    /// </summary>
    [EntitySystemOf(typeof(BuffChangeNumericRecordComponent))]
    public static partial class BuffChangeNumericRecordComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffChangeNumericRecordComponent self)
        {
        }

        /// <summary>
        /// 组件销毁时，遍历已记录的所有 <c>(NumericType, value)</c> 变更项并从宿主单位的 <see cref="NumericComponent"/> 中逆向还原。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffChangeNumericRecordComponent self)
        {
            Unit unit = self.Parent.Parent.GetParent<Unit>();
            if (unit.IsDisposed)
            {
                return;
            }
            NumericComponent numericComponent = unit.NumericComponent;

            for (int i = 0; i < self.Records.Count; i += 2)
            {
                NumericType numericType = (NumericType)self.Records[i];
                long value = self.Records[i + 1];
                numericComponent.Set(numericType, numericComponent.GetAsLong(numericType) - value);
            }
        }

        /// <summary>
        /// 按整型属性 ID 记录一次 Buff 施加的数值变更量。
        /// </summary>
        public static void Add(this BuffChangeNumericRecordComponent self, int numericType, long value)
        {
            self.Add((NumericType)numericType, value);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 记录一次 Buff 施加的数值变更量（在 <see cref="BuffChangeNumericRecordComponent.Records"/> 中成对存储类型与变更值）。
        /// </summary>
        public static void Add(this BuffChangeNumericRecordComponent self, NumericType numericType, long value)
        {
            self.Records.Add((long)numericType);
            self.Records.Add(value);
        }
    }
}
