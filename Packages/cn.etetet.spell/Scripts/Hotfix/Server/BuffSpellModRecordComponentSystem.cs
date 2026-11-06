namespace ET.Server
{
    /// <summary>
    /// Buff 技能修正（SpellMod）记录组件系统，记录该 Buff 对宿主技能修正值的改动，并在 Buff 移除时自动逆向还原。
    /// </summary>
    [EntitySystemOf(typeof(BuffSpellModRecordComponent))]
    public static partial class BuffSpellModRecordComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffSpellModRecordComponent self)
        {
        }

        /// <summary>
        /// 组件销毁时，遍历已记录的 <c>(spellId, spellModType, value)</c> 三元组，从宿主的 <see cref="SpellComponent"/> 中逆向减去修正值。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffSpellModRecordComponent self)
        {
            Unit unit = self.Parent.Parent.GetParent<Unit>();
            if (unit.IsDisposed)
            {
                return;
            }
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();

            for (int i = 0; i < self.Records.Count; i += 3)
            {
                int spellId = self.Records[i];
                SpellModType spellModType = (SpellModType)self.Records[i + 1];
                int value = self.Records[i + 2];
                spellComponent.AddMod(spellId, spellModType, -value);
            }
        }

        /// <summary>
        /// 记录一次针对特定技能 ID 与修正类型的改动值。
        /// </summary>
        public static void Add(this BuffSpellModRecordComponent self, int spellId, SpellModType spellModType, int value)
        {
            self.Records.Add(spellId);
            self.Records.Add((int)spellModType);
            self.Records.Add(value);
        }
    }
}