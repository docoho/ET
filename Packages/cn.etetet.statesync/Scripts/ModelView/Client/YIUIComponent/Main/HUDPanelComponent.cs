using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
    /// Desc    场景头顶信息容器面板（HUDPanel）数据组件，监听 <see cref="EventMain_ShowHPView"/> 动态事件挂载单位血条
    /// </summary>
    public partial class HUDPanelComponent : Entity, IDynamicEvent<EventMain_ShowHPView>
    {
        /// <summary>
        /// 本地玩家单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Unit;

        /// <summary>
        /// 获取本地玩家单位实例
        /// </summary>
        public Unit            Unit => m_Unit;

        /// <summary>
        /// 本地玩家单位配置数据
        /// </summary>
        public UnitConfig m_UnitConfig;
    }
}