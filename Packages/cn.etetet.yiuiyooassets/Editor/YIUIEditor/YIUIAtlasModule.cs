#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace YIUIFramework.Editor
{
    /// <summary>
    /// YIUI 编辑器图集模块扩展，提供打包所有 SpriteAtlas 并生成运行时全局图集映射配置资产（YIUIAtlasData）的功能。
    /// </summary>
    public partial class YIUIAtlasModule
    {
        /// <summary>
        /// 打包全部图集并扫描所有 .spriteatlasv2 文件，提取图集与散图对应关系并保存为 YIUIAtlasData.asset
        /// </summary>
        public static void RefreshAtlasData()
        {
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);

            //不需要检查是否有重复的 因为 YOOASSET 开了可寻址必须是唯一的
            var atlasInfos = new List<YIUIAtlasInfo>();
            var absolutePaths = Directory.GetFiles(YIUIConstHelper.Const.UIProjectResPath, "*.spriteatlasv2", SearchOption.AllDirectories);
            foreach (var absolutePath in absolutePaths)
            {
                var relativePath = absolutePath.Replace(Application.dataPath, "Assets");
                var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(relativePath);

                if (spriteAtlas == null || spriteAtlas.spriteCount <= 0)
                {
                    continue;
                }

                var atlasName = spriteAtlas.name;

                var atlasInfo = new YIUIAtlasInfo
                {
                    AtlasName = atlasName,
                    SpriteNames = new string[spriteAtlas.spriteCount]
                };

                var sprites = new Sprite[spriteAtlas.spriteCount];
                var spriteCount = spriteAtlas.GetSprites(sprites);
                for (var i = 0; i < spriteCount; i++)
                {
                    atlasInfo.SpriteNames[i] = sprites[i].name.Replace("(Clone)", string.Empty);
                }

                atlasInfos.Add(atlasInfo);
            }

            var atlasSavePath = YIUIConstAsset.AtlasDataPath;
            var atlasAssetPath = atlasSavePath.Replace("Assets", Application.dataPath);
            if (File.Exists(atlasAssetPath))
            {
                File.Delete(atlasAssetPath);
            }

            var kConfigMapAsset = ScriptableObject.CreateInstance<YIUIAtlasData>();
            kConfigMapAsset.Infos = atlasInfos.ToArray();
            AssetDatabase.CreateAsset(kConfigMapAsset, atlasSavePath);
            EditorUtility.SetDirty(kConfigMapAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif