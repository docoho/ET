using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
    /// Desc    2D 屏幕空间头顶血条（HPView）数据组件，缓存所属单位、头顶挂点 Transform、主角引用及数值组件引用
    /// </summary>
    public partial class HPViewComponent : Entity, ILateUpdate
    {
        /// <summary>
        /// 血条所属单位弱引用
        /// </summary>
        public EntityRef<Unit> m_OwnerUnit;

        /// <summary>
        /// 获取血条所属单位实例
        /// </summary>
        public Unit            OwnerUnit => m_OwnerUnit;

        /// <summary>
        /// 角色模型上的头顶血条绑点 Transform（<see cref="BindPoint.HP"/>）
        /// </summary>
        public Transform HPPoint;

        /// <summary>
        /// 本地玩家单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Player;

        /// <summary>
        /// 获取本地玩家单位实例
        /// </summary>
        public Unit            Player => m_Player;

        /// <summary>
        /// 血条所属单位的数值组件弱引用
        /// </summary>
        public EntityRef<NumericComponent> m_Numeric;

        /// <summary>
        /// 获取血条所属单位的数值组件实例
        /// </summary>
        public NumericComponent            Numeric => m_Numeric;
    }
}