using System.Collections.Generic;
using ET.Client;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// 技能编辑器表格行状态枚举（正常或缺失资产）。
    /// </summary>
    public enum SpellEditorRowKind
    {
        Normal,
        Missing,
    }

    /// <summary>
    /// 技能与 Buff 之间的引用来源类型枚举。
    /// </summary>
    public enum SpellEditorReferenceKind
    {
        MainSpell,
        SpellBuffId,
        CreateSpell,
        AddBuff,
    }

    /// <summary>
    /// 记录某个 Spell 或 Buff 被引用的来源信息（所属配置 ID、资产路径及行为树节点路径）。
    /// </summary>
    public sealed class SpellEditorSourceInfo
    {
        public SpellEditorReferenceKind Kind;
        public int OwnerId;
        public string OwnerPath;
        public string NodePath;

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.NodePath)
                    ? $"{this.Kind} {this.OwnerId}"
                    : $"{this.Kind} {this.OwnerId} @ {this.NodePath}";
        }
    }

    public sealed class SpellEditorSpellRow
    {
        public SpellEditorRowKind Kind;
        public int Id;
        public SpellScriptableObject Asset;
        public string AssetPath;
        public readonly List<SpellEditorSourceInfo> Sources = new();

        public SpellConfig Config => this.Asset != null? this.Asset.SpellConfig : null;
        public bool IsMain => SpellEditorConstants.IsMainSpell(this.Id);
    }

    public sealed class SpellEditorBuffRow
    {
        public SpellEditorRowKind Kind;
        public int Id;
        public BuffScriptableObject Asset;
        public string AssetPath;
        public readonly List<SpellEditorSourceInfo> Sources = new();

        public BuffConfig Config => this.Asset != null? this.Asset.BuffConfig : null;
    }

    public sealed class SpellEditorIssue
    {
        public MessageType Severity;
        public string Message;

        public SpellEditorIssue(MessageType severity, string message)
        {
            this.Severity = severity;
            this.Message = message;
        }
    }

    public sealed class SpellEditorBuildResult
    {
        public readonly List<SpellEditorSpellRow> Spells = new();
        public readonly List<SpellEditorBuffRow> Buffs = new();
        public readonly List<SpellEditorIssue> Issues = new();
    }
}
