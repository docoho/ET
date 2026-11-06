using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 表现层动作节点配置：在施法/引导期间通知 UI 显示施法读条进度条（如正向读条或反向引导）。
    /// </summary>
    public class BTShowCastSlider : BTAction
    {
        /// <summary>输入黑板变量名：关联的施法/引导 Buff（默认 "Buff"，用于读取起止时间计算进度）</summary>
        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        [ReadOnly]
        public string Buff = "Buff";

        /// <summary>进度条方向：true 为从 0% 增长到 100%（正向读条），false 为从 100% 递减到 0%（引导施法）</summary>
        [LabelText("是否增加施法进度条")]
        [InfoBox("如: 寒冰箭读条就是增长，暴风雪引导就是减少")]
        public bool IsIncrease = true;

        /// <summary>进度条上显示的技能/动作名称文本</summary>
#if UNITY
        [BoxGroup("显示信息", CenterLabel = true)]
        [LabelText("进度条显示名称")]
#endif
        public string ShowDisplayName;

        /// <summary>进度条旁显示的技能图标资源名称</summary>
#if UNITY
        [BoxGroup("显示信息")]
        [LabelText("进度条显示图标资源名")]
#endif
        public string IconName;
    }
    
    /// <summary>
    /// 通知表现层 UI 显示施法进度条的事件参数结构体。
    /// </summary>
    public struct BTEvent_ShowCastSlider
    {
        /// <summary>正在施法读条的单位引用</summary>
        public EntityRef<Unit> Unit;
        /// <summary>驱动读条时长的 Buff 引用</summary>
        public EntityRef<Buff> Buff;
        /// <summary>进度条是否为正向增长</summary>
        public bool IsIncrease;
        /// <summary>技能图标资源名称</summary>
        public string IconName;
        /// <summary>进度条显示的技能名称</summary>
        public string ShowDisplayName;
    }
}
