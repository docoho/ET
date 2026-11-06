using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc    主界面“目标的目标”信息框（TargetTargetInfo）数据组件，缓存本地玩家目标组件及“目标的目标”单位引用
    /// </summary>
    public partial class TargetTargetInfoComponent : Entity, ILateUpdate
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
        /// 当前“目标的目标”单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Unit;

        /// <summary>
        /// 获取当前“目标的目标”单位实例
        /// </summary>
        public Unit            Unit => m_Unit;

        /// <summary>
        /// 当前“目标的目标”单位配置表数据
        /// </summary>
        public UnitConfig m_UnitConfig;
        
        /// <summary>
        /// 组件当前是否处于激活显示状态
        /// </summary>
        public bool m_CurrentActiveState;
    }
}
