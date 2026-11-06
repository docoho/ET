using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using YIUIFramework;
using YooAsset;

namespace ET.Client
{
    /// <summary>
    /// YIUI YooAsset 精灵（Sprite）与图集（SpriteAtlas）加载组件系统，负责图集映射表的初始化以及透明化的散图/图集加载与释放。
    /// </summary>
    [FriendOf(typeof(YIUIYooAssetsSpriteComponent))]
    [EntitySystemOf(typeof(YIUIYooAssetsSpriteComponent))]
    public static partial class YIUIYooAssetsSpriteComponentSystem
    {
        /// <summary>
        /// 组件唤醒时缓存父级 YIUILoadComponent 引用
        /// </summary>
        [EntitySystem]
        private static void Awake(this YIUIYooAssetsSpriteComponent self)
        {
            self.m_YIUILoadRef = self.GetParent<YIUILoadComponent>();
        }

        /// <summary>
        /// 异步加载全局图集配置资产（YIUIAtlasData），构建散图名到所属图集名的快速查找字典
        /// </summary>
        public static async ETTask LoadAtlasAsync(this YIUIYooAssetsSpriteComponent self)
        {
            EntityRef<YIUIYooAssetsSpriteComponent> selfRef = self;
            var handle = YooAssets.LoadAssetAsync<YIUIAtlasData>(YIUIConstAsset.AtlasDataName);

            await handle.Task;

            var atlasData = handle.AssetObject as YIUIAtlasData;
            if (atlasData == null)
            {
                return;
            }

            self = selfRef;
            self.m_SpritePathMap.Clear();
            foreach (var info in atlasData.Infos)
            {
                foreach (var spriteName in info.SpriteNames)
                {
                    self.m_SpritePathMap.Add(spriteName, info.AtlasName);
                }
            }

            handle.Release();
        }

        /// <summary>
        /// 根据精灵名称查询其所属的图集名称（若不属于任何图集则返回 null）
        /// </summary>
        public static string GetSpriteAtlasPath(this YIUIYooAssetsSpriteComponent self, string spriteName)
        {
            return self.m_SpritePathMap.GetValueOrDefault(spriteName);
        }

        #if !YIUIMACRO_SYNCLOAD_CLOSE
        /// <summary>
        /// 同步加载指定名称的 Sprite（若在图集映射表中则先加载图集再提取 Sprite，否则直接按散图加载）
        /// </summary>
        public static Sprite GetSprite(this YIUIYooAssetsSpriteComponent self, string spriteName)
        {
            Sprite sprite = null;

            if (!self.m_SpritePathMap.TryGetValue(spriteName, out string atlasPath))
            {
                #if UNITY_EDITOR
                if (!self.YIUILoad.VerifyAssetValidity(spriteName))
                {
                    Log.Error($"验证资产有效性 没有这个资源 图片无法加载 请检查 {spriteName}");
                    return null;
                }
                #endif

                sprite = self.YIUILoad.LoadAsset<Sprite>(spriteName);

                if (sprite == null)
                {
                    Log.Error($"加载失败 没有这个资源 图片无法加载 请检查 {spriteName}");
                }
            }
            else
            {
                var spriteAtlas = self.YIUILoad.LoadAsset<SpriteAtlas>(atlasPath);
                sprite = spriteAtlas?.GetSprite(spriteName);
                if (sprite == null)
                {
                    Log.Error($"找到图集{atlasPath},但是没有找不到Sprite：{spriteName}");
                    return null;
                }

                if (!self.m_LoadedSprites.TryAdd(sprite, spriteAtlas))
                {
                    Log.Error($"重复添加Sprite：{spriteName}");
                }
            }

            return sprite;
        }
        #endif

        /// <summary>
        /// 异步加载指定名称的 Sprite（若在图集映射表中则先异步加载图集再提取 Sprite，否则直接按散图异步加载）
        /// </summary>
        public static async ETTask<Sprite> GetSpriteAsync(this YIUIYooAssetsSpriteComponent self, string spriteName)
        {
            Sprite sprite = null;

            if (!self.m_SpritePathMap.TryGetValue(spriteName, out string atlasPath))
            {
                #if UNITY_EDITOR
                if (!self.YIUILoad.VerifyAssetValidity(spriteName))
                {
                    Log.Error($"验证资产有效性 没有这个资源 图片无法加载 请检查 {spriteName}");
                    return null;
                }
                #endif

                sprite = await self.YIUILoad.LoadAssetAsync<Sprite>(spriteName);

                if (sprite == null)
                {
                    Log.Error($"加载失败 没有这个资源 图片无法加载 请检查 {spriteName}");
                }
            }
            else
            {
                EntityRef<YIUIYooAssetsSpriteComponent> selfRef = self;
                var spriteAtlas = await self.YIUILoad.LoadAssetAsync<SpriteAtlas>(atlasPath);
                sprite = spriteAtlas?.GetSprite(spriteName);
                if (sprite == null)
                {
                    Log.Error($"找到图集{atlasPath},但是没有找不到Sprite：{spriteName}");
                    return null;
                }

                self = selfRef;
                if (!self.m_LoadedSprites.TryAdd(sprite, spriteAtlas))
                {
                    Log.Error($"重复添加Sprite：{spriteName}");
                }
            }

            return sprite;
        }

        /// <summary>
        /// 释放指定的 Sprite（若是从图集提取的 Sprite，则释放对应的图集引用计数；否则直接释放散图资源）
        /// </summary>
        public static void ReleaseSprite(this YIUIYooAssetsSpriteComponent self, Sprite sprite)
        {
            if (self.m_LoadedSprites.ContainsKey(sprite))
            {
                var spriteAtlas = self.m_LoadedSprites[sprite];
                self.m_LoadedSprites.Remove(sprite);
                self.YIUILoad.Release(spriteAtlas);
            }
            else
            {
                self.YIUILoad.Release(sprite);
            }
        }
    }
}