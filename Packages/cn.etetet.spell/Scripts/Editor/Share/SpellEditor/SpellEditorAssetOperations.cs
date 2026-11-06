using System.Collections.Generic;
using System.IO;
using System.Linq;
using ET.Client;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 技能编辑器资产操作工具类，提供创建、删除、修改 ID（含自动重写行为树内引用）、整条技能链路深拷贝以及导出 BSON 配置的功能。
    /// </summary>
    public static class SpellEditorAssetOperations
    {
        public static void SaveAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 保存资产并将所有 ScriptableObject 导出为运行时 BSON 配置文件。
        /// </summary>
        public static void ExportConfig()
        {
            SaveAssets();
            ExportScriptableObjectEditor.ExportScriptableObject();
        }

        /// <summary>
        /// 在行为树编辑器窗口中打开指定的根节点（Cost、TargetSelector 或 EffectNode）。
        /// </summary>
        public static void OpenBehaviorTree(UnityEngine.Object owner, BTRoot root)
        {
            if (owner == null || root == null)
            {
                return;
            }

            BTNodeDrawer.OpenNode = root;
            BTNodeDrawer.ScriptableObject = owner;
            EditorApplication.ExecuteMenuItem("ET/BehaviorTree/BehaviorTreeEditor");
        }

        public static int GetAssetId(UnityEngine.Object asset)
        {
            return asset switch
            {
                SpellScriptableObject spellAsset => spellAsset.SpellConfig.Id,
                BuffScriptableObject buffAsset => buffAsset.BuffConfig.Id,
                _ => 0,
            };
        }

        public static string GetAssetDirectory(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string directory = Path.GetDirectoryName(path);
            return directory?.Replace("\\", "/") ?? SpellEditorConstants.BtAssetRoot;
        }

        public static string GetNewMainSpellDirectory(int mainSpellId)
        {
            string baseDirectory = $"{SpellEditorConstants.BtAssetRoot}/{mainSpellId}-NewSpell";
            string directory = baseDirectory;
            int duplicateIndex = 2;
            while (Directory.Exists(directory))
            {
                directory = $"{baseDirectory}-{duplicateIndex}";
                duplicateIndex++;
            }

            return directory;
        }

        public static SpellScriptableObject CreateSpellAsset(int spellId, string directory)
        {
            SpellConfig config = new()
            {
                Id = spellId,
            };
            return CreateSpellAsset(config, directory);
        }

        public static BuffScriptableObject CreateBuffAsset(int buffId, string directory)
        {
            BuffConfig config = new()
            {
                Id = buffId,
            };
            return CreateBuffAsset(config, directory);
        }

        public static SpellScriptableObject CreateSpellAsset(SpellConfig config, string directory)
        {
            if (config == null || config.Id <= 0)
            {
                EditorUtility.DisplayDialog("创建失败", "Spell Id 必须大于 0。", "确定");
                return null;
            }

            SpellEditorAssetIndex index = SpellEditorAssetIndex.Build();
            if (!EnsureSpellCreationIdAvailable(index, config.Id, "创建失败"))
            {
                return null;
            }

            EnsureDirectory(directory);
            string path = $"{directory}/{SpellEditorConstants.SpellAssetName(config.Id)}.asset";
            if (!EnsurePathAvailable(path))
            {
                return null;
            }

            SpellScriptableObject asset = ScriptableObject.CreateInstance<SpellScriptableObject>();
            asset.SpellConfig = config;
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            SaveAssets();
            return asset;
        }

        public static BuffScriptableObject CreateBuffAsset(BuffConfig config, string directory)
        {
            if (config == null || config.Id <= 0)
            {
                EditorUtility.DisplayDialog("创建失败", "Buff Id 必须大于 0。", "确定");
                return null;
            }

            SpellEditorAssetIndex index = SpellEditorAssetIndex.Build();
            if (index.Buffs.ContainsKey(config.Id))
            {
                EditorUtility.DisplayDialog("创建失败", $"Buff Id {config.Id} 已存在。", "确定");
                return null;
            }

            EnsureDirectory(directory);
            string path = $"{directory}/{SpellEditorConstants.BuffAssetName(config.Id)}.asset";
            if (!EnsurePathAvailable(path))
            {
                return null;
            }

            BuffScriptableObject asset = ScriptableObject.CreateInstance<BuffScriptableObject>();
            asset.BuffConfig = config;
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            SaveAssets();
            return asset;
        }

        public static bool DeleteAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            bool ok = AssetDatabase.DeleteAsset(path);
            SaveAssets();
            return ok;
        }

        public static bool RenameAssetId(SpellEditorAssetIndex index, UnityEngine.Object asset, int newId)
        {
            if (index == null || asset == null)
            {
                return false;
            }

            if (newId <= 0)
            {
                EditorUtility.DisplayDialog("改 Id 失败", "目标 Id 必须大于 0。", "确定");
                return false;
            }

            if (asset is SpellScriptableObject spellAsset)
            {
                int oldId = spellAsset.SpellConfig.Id;
                if (oldId == newId)
                {
                    return true;
                }

                if (index.Spells.ContainsKey(newId))
                {
                    EditorUtility.DisplayDialog("改 Id 失败", $"Spell Id {newId} 已存在。", "确定");
                    return false;
                }

                bool hasPrimaryBuff = index.Buffs.TryGetValue(oldId, out BuffScriptableObject primaryBuffAsset);
                if (hasPrimaryBuff && index.Buffs.ContainsKey(newId))
                {
                    EditorUtility.DisplayDialog("改 Id 失败", $"主 Buff Id {newId} 已存在，无法随 Spell 同步改 Id。", "确定");
                    return false;
                }

                if (!EnsureRenameTargetAvailable(spellAsset, SpellEditorConstants.SpellAssetName(newId), "改 Id 失败"))
                {
                    return false;
                }

                if (hasPrimaryBuff && !EnsureRenameTargetAvailable(primaryBuffAsset, SpellEditorConstants.BuffAssetName(newId), "改 Id 失败"))
                {
                    return false;
                }

                spellAsset.SpellConfig.Id = newId;
                Dictionary<int, int> buffIdMap = null;
                if (hasPrimaryBuff)
                {
                    primaryBuffAsset.BuffConfig.Id = newId;
                    buffIdMap = new Dictionary<int, int> { [oldId] = newId };
                    EditorUtility.SetDirty(primaryBuffAsset);
                }

                EditorUtility.SetDirty(spellAsset);
                RewriteReferences(index, new Dictionary<int, int> { [oldId] = newId }, buffIdMap);
                if (!RenameAssetFile(spellAsset))
                {
                    return false;
                }

                if (hasPrimaryBuff && !RenameAssetFile(primaryBuffAsset))
                {
                    return false;
                }

                SaveAssets();
                return true;
            }

            if (asset is BuffScriptableObject buffAsset)
            {
                int oldId = buffAsset.BuffConfig.Id;
                if (oldId == newId)
                {
                    return true;
                }

                if (index.Buffs.ContainsKey(newId))
                {
                    EditorUtility.DisplayDialog("改 Id 失败", $"Buff Id {newId} 已存在。", "确定");
                    return false;
                }

                if (!EnsureRenameTargetAvailable(buffAsset, SpellEditorConstants.BuffAssetName(newId), "改 Id 失败"))
                {
                    return false;
                }

                buffAsset.BuffConfig.Id = newId;
                RewriteReferences(index, null, new Dictionary<int, int> { [oldId] = newId });
                EditorUtility.SetDirty(buffAsset);
                if (!RenameAssetFile(buffAsset))
                {
                    return false;
                }

                SaveAssets();
                return true;
            }

            return false;
        }

        public static int FindNextSubSpellId(SpellEditorAssetIndex index, int mainSpellId)
        {
            int start = SpellEditorConstants.MainSpellBase(mainSpellId) + 1;
            int end = SpellEditorConstants.MainSpellBase(mainSpellId) + SpellEditorConstants.SpellGroupSize - 1;
            for (int id = start; id <= end; id++)
            {
                if (IsSpellCreationIdAvailable(index, id))
                {
                    return id;
                }
            }

            return 0;
        }

        public static int FindNextMainSpellId(SpellEditorAssetIndex index, int sourceMainSpellId)
        {
            int candidate = SpellEditorConstants.MainSpellBase(sourceMainSpellId) + SpellEditorConstants.SpellGroupSize;
            while (candidate <= int.MaxValue - SpellEditorConstants.SpellGroupSize)
            {
                if (IsSpellCreationIdAvailable(index, candidate))
                {
                    return candidate;
                }

                candidate += SpellEditorConstants.SpellGroupSize;
            }

            return 0;
        }

        public static int FindNextBuffId(SpellEditorAssetIndex index, int baseSpellId)
        {
            int id = baseSpellId;
            while (index.Buffs.ContainsKey(id))
            {
                id++;
            }

            return id;
        }

        public static bool CopySpellChain(SpellEditorAssetIndex index, SpellEditorBuildResult buildResult, int sourceMainSpellId, int targetMainSpellId)
        {
            if (index == null || buildResult == null)
            {
                return false;
            }

            if (!SpellEditorConstants.IsMainSpell(targetMainSpellId))
            {
                EditorUtility.DisplayDialog("复制失败", "目标主技能 Id 必须是 10 的倍数。", "确定");
                return false;
            }

            if (index.Spells.ContainsKey(targetMainSpellId))
            {
                EditorUtility.DisplayDialog("复制失败", $"Spell Id {targetMainSpellId} 已存在。", "确定");
                return false;
            }

            if (index.Buffs.ContainsKey(targetMainSpellId))
            {
                EditorUtility.DisplayDialog("复制失败", $"同 Id Buff {targetMainSpellId} 已存在，无法创建目标主 Spell。", "确定");
                return false;
            }

            List<SpellEditorSpellRow> spellRows = buildResult.Spells.Where(x => x.Asset != null).ToList();
            List<SpellEditorBuffRow> buffRows = buildResult.Buffs.Where(x => x.Asset != null).ToList();
            if (spellRows.Count == 0 || !spellRows.Any(x => x.Id == sourceMainSpellId))
            {
                EditorUtility.DisplayDialog("复制失败", "当前链路没有可复制的主技能。", "确定");
                return false;
            }

            if (!TryBuildSpellIdMap(index, spellRows, sourceMainSpellId, targetMainSpellId, out Dictionary<int, int> spellIdMap))
            {
                return false;
            }

            if (!TryBuildBuffIdMap(index, spellRows, buffRows, spellIdMap, targetMainSpellId, out Dictionary<int, int> buffIdMap))
            {
                return false;
            }

            string targetDirectory = GetCopyDirectory(index, sourceMainSpellId, targetMainSpellId);

            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureDirectory(targetDirectory);
                foreach (SpellEditorSpellRow row in spellRows)
                {
                    SpellConfig config = CloneValue(row.Asset.SpellConfig);
                    config.Id = spellIdMap[row.Id];

                    RewriteNodeReferences(config.Cost, spellIdMap, buffIdMap);
                    RewriteNodeReferences(config.TargetSelector, spellIdMap, buffIdMap);
                    CreateSpellAssetWithoutRefresh(config, targetDirectory);
                }

                foreach (SpellEditorBuffRow row in buffRows)
                {
                    BuffConfig config = CloneValue(row.Asset.BuffConfig);
                    config.Id = buffIdMap[row.Id];
                    config.effectDict = null;
                    if (config.Effects != null)
                    {
                        foreach (EffectNode effect in config.Effects)
                        {
                            RewriteNodeReferences(effect, spellIdMap, buffIdMap);
                        }
                    }

                    CreateBuffAssetWithoutRefresh(config, targetDirectory);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            SaveAssets();
            return true;
        }

        public static bool IsSpellCreationIdAvailable(SpellEditorAssetIndex index, int spellId)
        {
            return index != null && !index.Spells.ContainsKey(spellId) && !index.Buffs.ContainsKey(spellId);
        }

        private static bool RenameAssetFile(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string error = AssetDatabase.RenameAsset(path, GetAssetName(asset));
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("重命名失败", error, "确定");
                return false;
            }

            EditorUtility.SetDirty(asset);
            return true;
        }

        private static bool EnsureSpellCreationIdAvailable(SpellEditorAssetIndex index, int spellId, string title)
        {
            if (index.Spells.ContainsKey(spellId))
            {
                EditorUtility.DisplayDialog(title, $"Spell Id {spellId} 已存在。", "确定");
                return false;
            }

            if (index.Buffs.ContainsKey(spellId))
            {
                EditorUtility.DisplayDialog(title, $"同 Id Buff {spellId} 已存在，无法创建 Spell。", "确定");
                return false;
            }

            return true;
        }

        private static bool EnsureRenameTargetAvailable(UnityEngine.Object asset, string targetName, string title)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string directory = Path.GetDirectoryName(path)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory))
            {
                EditorUtility.DisplayDialog(title, "无法获取当前资产目录。", "确定");
                return false;
            }

            string targetPath = $"{directory}/{targetName}.asset";
            UnityEngine.Object targetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
            if (targetAsset != null && targetAsset != asset)
            {
                EditorUtility.DisplayDialog(title, $"目标资产已存在：{targetPath}", "确定");
                return false;
            }

            return true;
        }

        private static string GetAssetName(UnityEngine.Object asset)
        {
            return asset switch
            {
                SpellScriptableObject spellAsset => SpellEditorConstants.SpellAssetName(spellAsset.SpellConfig.Id),
                BuffScriptableObject buffAsset => SpellEditorConstants.BuffAssetName(buffAsset.BuffConfig.Id),
                _ => asset.name,
            };
        }

        private static void RewriteReferences(SpellEditorAssetIndex index, Dictionary<int, int> spellIdMap, Dictionary<int, int> buffIdMap)
        {
            foreach (SpellScriptableObject spellAsset in index.Spells.Values)
            {
                if (spellAsset == null)
                {
                    continue;
                }

                bool dirty = false;
                SpellConfig config = spellAsset.SpellConfig;
                dirty |= RewriteNodeReferences(config.Cost, spellIdMap, buffIdMap);
                dirty |= RewriteNodeReferences(config.TargetSelector, spellIdMap, buffIdMap);
                if (dirty)
                {
                    EditorUtility.SetDirty(spellAsset);
                }
            }

            foreach (BuffScriptableObject buffAsset in index.Buffs.Values)
            {
                if (buffAsset?.BuffConfig.Effects == null)
                {
                    continue;
                }

                bool dirty = false;
                foreach (EffectNode effect in buffAsset.BuffConfig.Effects)
                {
                    dirty |= RewriteNodeReferences(effect, spellIdMap, buffIdMap);
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(buffAsset);
                }
            }
        }

        private static bool RewriteNodeReferences(BTNode node, Dictionary<int, int> spellIdMap, Dictionary<int, int> buffIdMap)
        {
            if (node == null)
            {
                return false;
            }

            bool dirty = false;
            if (spellIdMap != null && node is BTCreateSpell createSpell && spellIdMap.TryGetValue(createSpell.SpellConfigId, out int mappedSpellId))
            {
                createSpell.SpellConfigId = mappedSpellId;
                dirty = true;
            }

            if (buffIdMap != null && node is BTAddBuff addBuff && buffIdMap.TryGetValue(addBuff.ConfigId, out int mappedBuffId))
            {
                addBuff.ConfigId = mappedBuffId;
                dirty = true;
            }

            if (node.Children == null)
            {
                return dirty;
            }

            foreach (BTNode child in node.Children)
            {
                dirty |= RewriteNodeReferences(child, spellIdMap, buffIdMap);
            }

            return dirty;
        }

        private static bool TryBuildSpellIdMap(
            SpellEditorAssetIndex index,
            List<SpellEditorSpellRow> spellRows,
            int sourceMainSpellId,
            int targetMainSpellId,
            out Dictionary<int, int> spellIdMap)
        {
            spellIdMap = new Dictionary<int, int>();
            HashSet<int> reserved = index.Spells.Keys.Concat(index.Buffs.Keys).ToHashSet();
            int sourceBase = SpellEditorConstants.MainSpellBase(sourceMainSpellId);
            int targetBase = SpellEditorConstants.MainSpellBase(targetMainSpellId);

            foreach (SpellEditorSpellRow row in spellRows.OrderBy(x => x.Id == sourceMainSpellId ? 0 : 1).ThenBy(x => x.Id))
            {
                int targetId;
                if (row.Id == sourceMainSpellId)
                {
                    targetId = targetMainSpellId;
                    if (reserved.Contains(targetId))
                    {
                        EditorUtility.DisplayDialog("复制失败", $"目标主 Spell Id {targetId} 已被 Spell 或 Buff 占用。", "确定");
                        return false;
                    }
                }
                else if (row.Id >= sourceBase && row.Id < sourceBase + SpellEditorConstants.SpellGroupSize)
                {
                    int offset = row.Id - sourceBase;
                    targetId = targetBase + offset;
                    if (reserved.Contains(targetId))
                    {
                        targetId = FindNextAvailableSubSpellId(reserved, targetBase);
                    }
                }
                else
                {
                    targetId = FindNextAvailableSubSpellId(reserved, targetBase);
                }

                if (targetId == 0)
                {
                    EditorUtility.DisplayDialog("复制失败", $"目标主技能组 {targetBase} 没有足够的子技能 Id。", "确定");
                    return false;
                }

                spellIdMap[row.Id] = targetId;
                reserved.Add(targetId);
            }

            return true;
        }

        private static bool TryBuildBuffIdMap(
            SpellEditorAssetIndex index,
            List<SpellEditorSpellRow> spellRows,
            List<SpellEditorBuffRow> buffRows,
            Dictionary<int, int> spellIdMap,
            int targetMainSpellId,
            out Dictionary<int, int> buffIdMap)
        {
            buffIdMap = new Dictionary<int, int>();
            HashSet<int> reserved = index.Buffs.Keys.ToHashSet();
            HashSet<int> primaryBuffIds = spellRows.Select(x => x.Id).ToHashSet();
            int fallbackBuffAssetId = targetMainSpellId;

            foreach (SpellEditorBuffRow row in buffRows.OrderBy(x => primaryBuffIds.Contains(x.Id) ? 0 : 1).ThenBy(x => x.Id))
            {
                SpellEditorSpellRow ownerSpell = spellRows.FirstOrDefault(x => x.Id == row.Id);
                int targetId = 0;
                if (ownerSpell != null && spellIdMap.TryGetValue(ownerSpell.Id, out int mappedSpellId))
                {
                    targetId = mappedSpellId;
                    if (reserved.Contains(targetId) || buffIdMap.ContainsValue(targetId))
                    {
                        EditorUtility.DisplayDialog("复制失败", $"目标主 Buff Id {targetId} 已存在。", "确定");
                        return false;
                    }
                }

                if (targetId == 0)
                {
                    targetId = fallbackBuffAssetId;
                }

                while (reserved.Contains(targetId) || buffIdMap.ContainsValue(targetId))
                {
                    targetId++;
                }

                buffIdMap[row.Id] = targetId;
                reserved.Add(targetId);
                fallbackBuffAssetId = targetId + 1;
            }

            return true;
        }

        private static int FindNextAvailableSubSpellId(HashSet<int> reserved, int mainSpellBase)
        {
            int start = mainSpellBase + 1;
            int end = mainSpellBase + SpellEditorConstants.SpellGroupSize - 1;
            for (int id = start; id <= end; id++)
            {
                if (!reserved.Contains(id))
                {
                    return id;
                }
            }

            return 0;
        }

        private static string GetCopyDirectory(SpellEditorAssetIndex index, int sourceMainSpellId, int targetMainSpellId)
        {
            string sourceDirectory = index.Spells.TryGetValue(sourceMainSpellId, out SpellScriptableObject sourceAsset)
                    ? GetAssetDirectory(sourceAsset)
                    : SpellEditorConstants.BtAssetRoot;
            string parent = Path.GetDirectoryName(sourceDirectory)?.Replace("\\", "/") ?? SpellEditorConstants.BtAssetRoot;
            string sourceName = Path.GetFileName(sourceDirectory);
            string suffix = sourceName;
            int dashIndex = sourceName.IndexOf('-');
            if (dashIndex >= 0 && dashIndex < sourceName.Length - 1)
            {
                suffix = sourceName[(dashIndex + 1)..];
            }

            string baseDirectory = $"{parent}/{targetMainSpellId}-{suffix}";
            string directory = baseDirectory;
            int duplicateIndex = 2;
            while (Directory.Exists(directory))
            {
                directory = $"{baseDirectory}-{duplicateIndex}";
                duplicateIndex++;
            }

            return directory;
        }

        private static SpellScriptableObject CreateSpellAssetWithoutRefresh(SpellConfig config, string directory)
        {
            SpellScriptableObject asset = ScriptableObject.CreateInstance<SpellScriptableObject>();
            asset.SpellConfig = config;
            AssetDatabase.CreateAsset(asset, $"{directory}/{SpellEditorConstants.SpellAssetName(config.Id)}.asset");
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BuffScriptableObject CreateBuffAssetWithoutRefresh(BuffConfig config, string directory)
        {
            BuffScriptableObject asset = ScriptableObject.CreateInstance<BuffScriptableObject>();
            asset.BuffConfig = config;
            AssetDatabase.CreateAsset(asset, $"{directory}/{SpellEditorConstants.BuffAssetName(config.Id)}.asset");
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T CloneValue<T>(T value)
        {
            byte[] bytes = Sirenix.Serialization.SerializationUtility.SerializeValue(value, DataFormat.Binary);
            return Sirenix.Serialization.SerializationUtility.DeserializeValue<T>(bytes, DataFormat.Binary);
        }

        private static void EnsureDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        private static bool EnsurePathAvailable(string path)
        {
            if (!File.Exists(path))
            {
                return true;
            }

            EditorUtility.DisplayDialog("创建失败", $"资产已存在：{path}", "确定");
            return false;
        }
    }
}
