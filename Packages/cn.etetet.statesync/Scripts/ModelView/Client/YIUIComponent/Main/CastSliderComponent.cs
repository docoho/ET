using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc    技能施法/引导读条（CastSlider）UI 数据组件，记录当前正在读条的 Buff、玩家引用及正向/反向进度模式
    /// </summary>
    public partial class CastSliderComponent : Entity, ILateUpdate, IDynamicEvent<BTEvent_ShowCastSlider>
    {
        /// <summary>
        /// 本地玩家单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Player;

        /// <summary>
        /// 获取本地玩家单位实例
        /// </summary>
        public Unit            Player => m_Player;

        /// <summary>
        /// 当前驱动读条显示的施法/引导 Buff 弱引用
        /// </summary>
        public EntityRef<Buff> m_CurrentBuff;

        /// <summary>
        /// 获取当前驱动读条的 Buff 实例
        /// </summary>
        public Buff            CurrentBuff => m_CurrentBuff;

        /// <summary>
        /// 读条 UI 当前是否处于激活显示状态
        /// </summary>
        public bool m_CurrentActiveState;

        /// <summary>
        /// 读条进度方向：true 为从 0 到 1 递增（施法吟唱），false 为从 1 到 0 递减（持续引导）
        /// </summary>
        public bool m_IsIncrease;
    }
}