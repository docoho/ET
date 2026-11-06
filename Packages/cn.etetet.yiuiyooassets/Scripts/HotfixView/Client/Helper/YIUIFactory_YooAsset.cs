using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// YIUI 工厂类 YooAsset 扩展，提供省略 pkgName 参数的 GameObject 异步实例化便捷接口。
    /// </summary>
    public static partial class YIUIFactory
    {
        /// <summary>
        /// 异步加载并实例化指定资源名称的 UI 预制体 GameObject（省略包名）
        /// </summary>
        public static async ETTask<GameObject> InstantiateGameObjectAsync(Scene scene, string resName)
        {
            return await InstantiateGameObjectAsync(scene, "", resName);
        }
    }
}