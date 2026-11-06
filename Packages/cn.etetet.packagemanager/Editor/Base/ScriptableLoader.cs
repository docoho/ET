using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// ScriptableObject 资产全局查找与加载辅助类
    /// </summary>
    public static class ScriptableLoader
    {
        /// <summary>
        /// 在 AssetDatabase 中按类型查找并加载唯一的 ScriptableObject 实例
        /// </summary>
        public static T Load<T>() where T : ScriptableObject
        {
            var settingType = typeof(T);
            var guids       = AssetDatabase.FindAssets($"t:{settingType.Name}");
            if (guids.Length == 0)
            {
                Debug.LogError($"没有这个文件 : {settingType.Name}");
                return null;
            }
            else
            {
                if (guids.Length != 1)
                {
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Debug.LogError($"找到多个文件 : {path}");
                    }
                }

                string filePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var    asset    = AssetDatabase.LoadAssetAtPath<T>(filePath);
                return asset;
            }
        }
    }
}