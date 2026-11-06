using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 技能与 Buff 行为树引用扫描器，递归遍历行为树节点，提取 BTCreateSpell 与 BTAddBuff 所指向的目标 ID。
    /// </summary>
    public static class SpellEditorReferenceScanner
    {
        public static void ScanSpell(SpellConfig spellConfig, string ownerPath, List<SpellEditorReference> results)
        {
            if (spellConfig == null)
            {
                return;
            }

            ScanNode(spellConfig.Cost, spellConfig.Id, ownerPath, "Cost", results);
            ScanNode(spellConfig.TargetSelector, spellConfig.Id, ownerPath, "TargetSelector", results);
        }

        public static void ScanBuff(BuffConfig buffConfig, string ownerPath, List<SpellEditorReference> results)
        {
            if (buffConfig?.Effects == null)
            {
                return;
            }

            for (int i = 0; i < buffConfig.Effects.Count; i++)
            {
                ScanNode(buffConfig.Effects[i], buffConfig.Id, ownerPath, $"Effects[{i}]", results);
            }
        }

        private static void ScanNode(BTNode node, int ownerId, string ownerPath, string nodePath, List<SpellEditorReference> results)
        {
            if (node == null)
            {
                return;
            }

            if (node is BTCreateSpell createSpell)
            {
                results.Add(new SpellEditorReference
                {
                    Kind = SpellEditorReferenceKind.CreateSpell,
                    OwnerId = ownerId,
                    OwnerPath = ownerPath,
                    NodePath = nodePath,
                    TargetId = createSpell.SpellConfigId,
                });
            }

            if (node is BTAddBuff addBuff)
            {
                results.Add(new SpellEditorReference
                {
                    Kind = SpellEditorReferenceKind.AddBuff,
                    OwnerId = ownerId,
                    OwnerPath = ownerPath,
                    NodePath = nodePath,
                    TargetId = addBuff.ConfigId,
                });
            }

            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                ScanNode(node.Children[i], ownerId, ownerPath, $"{nodePath}/Children[{i}]", results);
            }
        }
    }
}
