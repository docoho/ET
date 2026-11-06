namespace ET
{
    /// <summary>
    /// 技能或 Buff 行为树内部对其他 Spell/Buff 的单条引用记录。
    /// </summary>
    public sealed class SpellEditorReference
    {
        public SpellEditorReferenceKind Kind;
        public int OwnerId;
        public string OwnerPath;
        public string NodePath;
        public int TargetId;
    }
}
