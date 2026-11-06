using System;
using System.Collections.Generic;
using YooAsset;

namespace ET.Client
{
    /// <summary>
    /// YIUI资源管理器 yooasset扩展
    /// （挂载在 YIUILoadComponent 下，维护 YooAsset 资源包实例与所有活跃的加载句柄表）
    /// </summary>
    [ComponentOf(typeof(YIUILoadComponent))]
    public class YIUIYooAssetsLoadComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 已加载资源的唯一 HashCode 到 YooAsset 资源句柄（AssetHandle）的映射表，用于按句柄释放
        /// </summary>
        public Dictionary<int, AssetHandle> m_AllHandle = new();

        /// <summary>
        /// 当前使用的 YooAsset 资源包（ResourcePackage）实例
        /// </summary>
        public ResourcePackage m_Package;
    }
}