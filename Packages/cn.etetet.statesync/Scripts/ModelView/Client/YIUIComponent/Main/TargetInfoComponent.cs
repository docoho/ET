using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc    主界面当前选中目标信息框（TargetInfo）数据组件，缓存本地玩家 <see cref="ET.Client.TargetComponent"/> 与当前目标单位引用
    /// </summary>
    public partial class TargetInfoComponent : Entity, ILateUpdate
    {
        /// <summary>
        /// 本地玩家的目标选择组件弱引用
        /// </summary>
        public EntityRef<TargetComponent> m_TargetComponent;

        /// <summary>
        /// 获取本地玩家的目标选择组件实例
        /// </summary>
        public TargetComponent            TargetComponent => m_TargetComponent;

        /// <summary>
        /// 当前选中的目标单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Unit;

        /// <summary>
        /// 获取当前选中的目标单位实例
        /// </summary>
        public Unit            Unit => m_Unit;

        /// <summary>
        /// 当前选中目标的单位配置表数据
        /// </summary>
        public UnitConfig m_UnitConfig;
        
        /// <summary>
        /// 目标信息框当前是否处于激活显示状态
        /// </summary>
        public bool m_CurrentActiveState;
    }
}