using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc    通用单位信息展示子组件（UnitInfo）数据模型，缓存当前展示的单位、数值组件与配置表数据，并实现 <see cref="IDynamicEvent{NumbericChange}"/> 监听数值变化
    /// </summary>
    public partial class UnitInfoComponent : Entity, IDynamicEvent<NumbericChange>
    {
        /// <summary>
        /// 当前展示的单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Unit;

        /// <summary>
        /// 获取当前展示的单位实例
        /// </summary>
        public Unit            Unit => m_Unit;

        /// <summary>
        /// 当前展示单位的数值组件弱引用
        /// </summary>
        public EntityRef<NumericComponent> m_Numeric;

        /// <summary>
        /// 获取当前展示单位的数值组件实例
        /// </summary>
        public NumericComponent            Numeric => m_Numeric;

        /// <summary>
        /// 当前展示单位的配置表数据
        /// </summary>
        public UnitConfig m_UnitConfig;
    }
}