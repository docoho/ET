using System;
using YooAsset;

namespace ET.Client
{
    /// <summary>
    /// YIUI资源管理器 yooasset扩展
    /// （将 YooAsset 的资源加载、句柄追踪、释放与校验能力桥接注入到 YIUILoadDI 委托中）
    /// </summary>
    [FriendOf(typeof(YIUIYooAssetsLoadComponent))]
    [EntitySystemOf(typeof(YIUIYooAssetsLoadComponent))]
    public static partial class YIUIYooAssetsLoadComponentSystem
    {
        #region ObjectSystem

        /// <summary>
        /// 组件唤醒生命周期回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this YIUIYooAssetsLoadComponent self)
        {
        }

        /// <summary>
        /// 组件销毁生命周期回调，释放所有尚未释放的 YooAsset 资源句柄
        /// </summary>
        [EntitySystem]
        private static void Destroy(this YIUIYooAssetsLoadComponent self)
        {
            self.ReleaseAllAction();
        }

        #endregion

        /// <summary>
        /// 初始化 YooAsset 资源包并注册 YIUILoadDI 全局资源加载/释放委托，完成后派发 YIUIEvent_YooAssetsLoad_Completed 事件
        /// </summary>
        /// <param name="self">YooAsset 加载扩展组件</param>
        /// <param name="packageName">YooAsset 资源包名称，默认 DefaultPackage</param>
        public static async ETTask<bool> Initialize(this YIUIYooAssetsLoadComponent self, string packageName = "DefaultPackage")
        {
            self.m_Package = YooAssets.GetPackage(packageName);
            if (self.m_Package == null)
            {
                Log.Error($"YooAsset 加载资源包失败: {packageName}");
                return false;
            }

            //YIUI会用到的各种加载 需要自行实现 当前是YooAsset 根据自己项目的资源管理器实现下面的方法
            #if !YIUIMACRO_SYNCLOAD_CLOSE
            YIUILoadDI.LoadAssetFunc = self.LoadAssetFunc; //同步加载
            #endif
            YIUILoadDI.LoadAssetAsyncFunc = self.LoadAssetAsyncFunc; //异步加载
            YIUILoadDI.ReleaseAction = self.ReleaseAction; //释放
            YIUILoadDI.VerifyAssetValidityFunc = self.VerifyAssetValidityFunc; //检查
            YIUILoadDI.ReleaseAllAction = self.ReleaseAllAction; //释放所有

            await EventSystem.Instance.PublishAsync(self.Scene(), new YIUIEvent_YooAssetsLoad_Completed
            {
                YooAssetsLoadRef = self
            });

            return true;
        }

        /// <summary>
        /// 释放方法
        /// </summary>
        /// <param name="hashCode">加载时所给到的唯一ID</param>
        private static void ReleaseAction(this YIUIYooAssetsLoadComponent self, int hashCode)
        {
            if (!self.m_AllHandle.TryGetValue(hashCode, out var value))
            {
                return;
            }

            value.Release();
            self.m_AllHandle.Remove(hashCode);
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        /// <param name="arg1">包名</param>
        /// <param name="arg2">资源名</param>
        /// <param name="arg3">类型</param>
        /// <returns>返回值(obj资源对象,唯一ID)</returns>
        private static async ETTask<(UnityEngine.Object, int)> LoadAssetAsyncFunc(this YIUIYooAssetsLoadComponent self,
                                                                                  string arg1,
                                                                                  string arg2,
                                                                                  Type arg3)
        {
            EntityRef<YIUIYooAssetsLoadComponent> selfRef = self;
            var handle = self.m_Package.LoadAssetAsync(arg2, arg3);
            await handle.Task;
            self = selfRef;
            return self.LoadAssetHandle(handle);
        }

        #if !YIUIMACRO_SYNCLOAD_CLOSE
        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="arg1">包名</param>
        /// <param name="arg2">资源名</param>
        /// <param name="arg3">类型</param>
        /// <returns>返回值(obj资源对象,唯一ID)</returns>
        private static (UnityEngine.Object, int) LoadAssetFunc(this YIUIYooAssetsLoadComponent self, string arg1, string arg2, Type arg3)
        {
            var handle = self.m_Package.LoadAssetSync(arg2, arg3);
            return self.LoadAssetHandle(handle);
        }
        #endif

        //Demo中对YooAsset加载后的一个简单返回封装
        //只有成功加载才返回 否则直接释放
        private static (UnityEngine.Object, int) LoadAssetHandle(this YIUIYooAssetsLoadComponent self, AssetHandle handle)
        {
            if (handle.AssetObject != null)
            {
                var hashCode = handle.GetHashCode();
                self.m_AllHandle.Add(hashCode, handle);
                return (handle.AssetObject, hashCode);
            }
            else
            {
                handle.Release();
                return (null, 0);
            }
        }

        //释放所有
        private static void ReleaseAllAction(this YIUIYooAssetsLoadComponent self)
        {
            foreach (var handle in self.m_AllHandle.Values)
            {
                handle.Release();
            }

            self.m_AllHandle.Clear();
        }

        //检查合法
        private static bool VerifyAssetValidityFunc(this YIUIYooAssetsLoadComponent self, string arg1, string arg2)
        {
            return self.m_Package.CheckLocationValid(arg2);
        }

        /// <summary>
        /// 获取当前的资源包
        /// </summary>
        public static ResourcePackage GetPackage(this YIUIYooAssetsLoadComponent self)
        {
            return self.m_Package;
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        public static AssetInfo GetAssetInfo(this YIUIYooAssetsLoadComponent self, string location, Type type = null)
        {
            return self.m_Package?.GetAssetInfo(location, type);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetGUID">资源GUID</param>
        /// <param name="type">资源类型</param>
        public static AssetInfo GetAssetInfoByGUID(this YIUIYooAssetsLoadComponent self, string assetGUID, Type type = null)
        {
            return self.m_Package?.GetAssetInfoByGUID(assetGUID, type);
        }
    }
}