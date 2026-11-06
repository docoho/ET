using System;
using UnityObject = UnityEngine.Object;

namespace ET.Client
{
    /// <summary>
    /// YooAsset扩展  因为他不需要pkgName
    /// （为 YIUILoadComponent 提供省略包名参数的无包名快捷加载重载接口）
    /// </summary>
    [FriendOf(typeof(YIUILoadComponent))]
    public static partial class YIUILoadComponentSystem
    {
        #if !YIUIMACRO_SYNCLOAD_CLOSE
        /// <summary>
        /// 同步加载指定名称与泛型类型的资源（省略 pkgName）
        /// </summary>
        internal static T LoadAsset<T>(this YIUILoadComponent self, string resName) where T : UnityObject
        {
            return self.LoadAsset<T>("", resName);
        }
        #endif

        /// <summary>
        /// 异步加载指定名称与泛型类型的资源（省略 pkgName）
        /// </summary>
        internal static async ETTask<T> LoadAssetAsync<T>(this YIUILoadComponent self, string resName) where T : UnityObject
        {
            return await self.LoadAssetAsync<T>("", resName);
        }

        /// <summary>
        /// 异步加载指定名称与泛型类型的资源并通过回调返回（省略 pkgName）
        /// </summary>
        internal static void LoadAssetAsync<T>(this YIUILoadComponent self, string resName, Action<T> action) where T : UnityObject
        {
            self.LoadAssetAsync<T>("", resName, action);
        }

        /// <summary>
        /// 校验指定资源名称在资源包中是否合法有效（省略 pkgName）
        /// </summary>
        internal static bool VerifyAssetValidity(this YIUILoadComponent self, string resName)
        {
            return self.VerifyAssetValidity("", resName);
        }

        #region 非泛型

        #if !YIUIMACRO_SYNCLOAD_CLOSE
        /// <summary>
        /// 非泛型同步加载指定名称与 Type 类型的资源（省略 pkgName）
        /// </summary>
        internal static UnityObject LoadAsset(this YIUILoadComponent self, string resName, Type assetType)
        {
            return self.LoadAsset("", resName, assetType);
        }
        #endif

        /// <summary>
        /// 非泛型异步加载指定名称与 Type 类型的资源（省略 pkgName）
        /// </summary>
        internal static async ETTask<UnityObject> LoadAssetAsync(this YIUILoadComponent self, string resName, Type assetType)
        {
            return await self.LoadAssetAsync("", resName, assetType);
        }

        /// <summary>
        /// 非泛型异步加载指定名称与 Type 类型的资源并通过回调返回（省略 pkgName）
        /// </summary>
        internal static void LoadAssetAsync(this YIUILoadComponent self, string resName, Type assetType, Action<UnityObject> action)
        {
            self.LoadAssetAsync("", resName, assetType, action);
        }

        #endregion
    }
}