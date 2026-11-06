using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.10
    /// Desc    3D 世界空间血条（HPView3D）数据组件，缓存血条所属单位与本地主角单位引用
    /// </summary>
    public partial class HPView3DComponent : Entity, ILateUpdate
    {
        /// <summary>
        /// 血条所属的角色/怪物单位弱引用
        /// </summary>
        public EntityRef<Unit> m_OwnerUnit;

        /// <summary>
        /// 获取血条所属单位实例
        /// </summary>
        public Unit            OwnerUnit => m_OwnerUnit;

        /// <summary>
        /// 本地玩家单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Player;

        /// <summary>
        /// 获取本地玩家单位实例
        /// </summary>
        public Unit            Player => m_Player;
    }
}