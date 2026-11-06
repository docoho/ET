using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.14
    /// Desc    主界面单个技能快捷栏槽位（ActionBarSlot）数据组件，缓存绑定的技能配置、主角单位、数值与技能组件引用
    /// </summary>
    public partial class ActionBarSlotComponent : Entity, ILateUpdate
    {
        /// <summary>
        /// 当前槽位绑定的技能配置数据
        /// </summary>
        public SpellConfig     m_SpellConfig;

        /// <summary>
        /// 本地玩家单位弱引用
        /// </summary>
        public EntityRef<Unit> m_Player;

        /// <summary>
        /// 获取本地玩家单位实例
        /// </summary>
        public Unit            Player => m_Player;

        /// <summary>
        /// 本地玩家数值组件弱引用
        /// </summary>
        public EntityRef<NumericComponent> m_Numeric;

        /// <summary>
        /// 获取本地玩家数值组件实例
        /// </summary>
        public NumericComponent            Numeric => m_Numeric;

        /// <summary>
        /// 本地玩家技能组件弱引用
        /// </summary>
        public EntityRef<SpellComponent> m_SpellComponent;

        /// <summary>
        /// 获取本地玩家技能组件实例
        /// </summary>
        public SpellComponent            SpellComponent => m_SpellComponent;

        /// <summary>
        /// 公共冷却时长（毫秒，默认 2000ms）
        /// </summary>
        public int GGCD = 2000; //公共CD SpellComponent 暂时写的2秒 所以这里写死 有需求再改
    }
}