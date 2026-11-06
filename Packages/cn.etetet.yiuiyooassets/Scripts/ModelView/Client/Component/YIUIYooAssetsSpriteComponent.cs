using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace ET.Client
{
    /// <summary>
    /// 有图集时用图集加载,否则正常加载
    /// 如果你要访问图集干其他事情 请自行实现 因为不利于统一管理
    /// 这里只负责加载图片
    /// </summary>
    [ComponentOf(typeof(YIUILoadComponent))]
    public class YIUIYooAssetsSpriteComponent : Entity, IAwake
    {
        /// <summary>
        /// 父级 YIUILoadComponent 实体安全引用
        /// </summary>
        public EntityRef<YIUILoadComponent> m_YIUILoadRef;

        /// <summary>
        /// 获取父级 YIUILoadComponent 实例
        /// </summary>
        public YIUILoadComponent YIUILoad => m_YIUILoadRef;

        /// <summary>
        /// 散图名称（SpriteName）到所属图集名称（AtlasName）的映射字典
        /// </summary>
        public readonly Dictionary<string, string> m_SpritePathMap = new();

        /// <summary>
        /// 已从图集中克隆提取出的 Sprite 实例到所属 SpriteAtlas 实例的映射表，用于释放 Sprite 时归还对应图集引用
        /// </summary>
        public readonly Dictionary<Sprite, SpriteAtlas> m_LoadedSprites = new();
    }
}