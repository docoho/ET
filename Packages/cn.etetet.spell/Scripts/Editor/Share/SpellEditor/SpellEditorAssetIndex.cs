using System.Collections.Generic;
using ET.Client;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 技能编辑器资产索引，扫描指定目录下的所有 SpellScriptableObject 和 BuffScriptableObject，并记录重复 ID 冲突路径。
    /// </summary>
    public sealed class SpellEditorAssetIndex
    {
        public readonly Dictionary<int, SpellScriptableObject> Spells = new();
        public readonly Dictionary<int, BuffScriptableObject> Buffs = new();
        public readonly Dictionary<int, List<string>> DuplicateSpellPaths = new();
        public readonly Dictionary<int, List<string>> DuplicateBuffPaths = new();
        public readonly Dictionary<UnityEngine.Object, string> AssetPaths = new();

        /// <summary>
        /// 扫描资产库并构建完整的 Spell 与 Buff 索引。
        /// </summary>
        public static SpellEditorAssetIndex Build()
        {
            SpellEditorAssetIndex index = new();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { SpellEditorConstants.BtAssetRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue;
                }

                index.AssetPaths[asset] = path;

                if (asset is SpellScriptableObject spellAsset)
                {
                    int id = spellAsset.SpellConfig.Id;
                    AddIndexedAsset(index.Spells, index.DuplicateSpellPaths, id, spellAsset, path);
                    continue;
                }

                if (asset is BuffScriptableObject buffAsset)
                {
                    int id = buffAsset.BuffConfig.Id;
                    AddIndexedAsset(index.Buffs, index.DuplicateBuffPaths, id, buffAsset, path);
                }
            }

            return index;
        }

        public string GetPath(UnityEngine.Object asset)
        {
            return asset != null && this.AssetPaths.TryGetValue(asset, out string path)? path : string.Empty;
        }

        private static void AddIndexedAsset<T>(
            Dictionary<int, T> map,
            Dictionary<int, List<string>> duplicates,
            int id,
            T asset,
            string path) where T: UnityEngine.Object
        {
            if (map.TryAdd(id, asset))
            {
                return;
            }

            if (!duplicates.TryGetValue(id, out List<string> paths))
            {
                paths = new List<string>();
                duplicates[id] = paths;
                paths.Add(AssetDatabase.GetAssetPath(map[id]));
            }

            paths.Add(path);
        }
    }
}
