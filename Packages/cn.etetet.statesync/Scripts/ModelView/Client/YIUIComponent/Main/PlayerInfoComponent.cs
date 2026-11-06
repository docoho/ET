using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.7
    /// Desc    主界面左上角本地玩家信息框（PlayerInfo）数据组件，缓存本地玩家单位及其配置表数据
    /// </summary>
    public partial class PlayerInfoComponent : Entity
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