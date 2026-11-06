using System.Collections.Generic;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// 技能配置校验器，检查 ID 重复、资产文件名与配置 ID 不一致、主/子技能 ID 规范以及缺失同 ID 主 Buff 等问题。
    /// </summary>
    public static class SpellEditorValidator
    {
        public static List<SpellEditorIssue> Validate(SpellEditorAssetIndex index, SpellEditorBuildResult buildResult)
        {
            List<SpellEditorIssue> issues = new();

            foreach ((int id, List<string> paths) in index.DuplicateSpellPaths)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"Spell Id 重复: {id} => {string.Join(", ", paths)}"));
            }

            foreach ((int id, List<string> paths) in index.DuplicateBuffPaths)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"Buff Id 重复: {id} => {string.Join(", ", paths)}"));
            }

            foreach (SpellEditorSpellRow row in buildResult.Spells)
            {
                ValidateSpellRow(index, row, issues);
            }

            foreach (SpellEditorBuffRow row in buildResult.Buffs)
            {
                ValidateBuffRow(row, issues);
            }

            issues.AddRange(buildResult.Issues);
            return issues;
        }

        private static void ValidateSpellRow(SpellEditorAssetIndex index, SpellEditorSpellRow row, List<SpellEditorIssue> issues)
        {
            if (row.Asset == null)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"缺失 SpellConfig: {row.Id}"));
                return;
            }

            SpellConfig config = row.Asset.SpellConfig;
            string expectedSpellAssetName = SpellEditorConstants.SpellAssetName(config.Id);
            if (row.Asset.name != expectedSpellAssetName)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"Spell 资产名与 Id 不一致: {row.AssetPath} name={row.Asset.name} expected={expectedSpellAssetName} id={config.Id}"));
            }

            bool rowIsMainSource = false;
            foreach (SpellEditorSourceInfo source in row.Sources)
            {
                if (source.Kind == SpellEditorReferenceKind.MainSpell)
                {
                    rowIsMainSource = true;
                    break;
                }
            }

            if (rowIsMainSource && !SpellEditorConstants.IsMainSpell(config.Id))
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"主技能 Id 必须满足 % 10 == 0: {config.Id}"));
            }

            if (!rowIsMainSource && !SpellEditorConstants.IsSubSpell(config.Id))
            {
                issues.Add(new SpellEditorIssue(MessageType.Warning, $"子技能 Id 通常应满足 % 10 != 0: {config.Id}"));
            }

            if (!index.Buffs.ContainsKey(config.Id))
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"Spell {config.Id} 缺失同 Id 主 Buff {config.Id}"));
            }
        }

        private static void ValidateBuffRow(SpellEditorBuffRow row, List<SpellEditorIssue> issues)
        {
            if (row.Asset == null)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"缺失 BuffConfig: {row.Id}"));
                return;
            }

            BuffConfig config = row.Asset.BuffConfig;
            string expectedBuffAssetName = SpellEditorConstants.BuffAssetName(config.Id);
            if (row.Asset.name != expectedBuffAssetName)
            {
                issues.Add(new SpellEditorIssue(MessageType.Error, $"Buff 资产名与 Id 不一致: {row.AssetPath} name={row.Asset.name} expected={expectedBuffAssetName} id={config.Id}"));
            }
        }
    }
}
