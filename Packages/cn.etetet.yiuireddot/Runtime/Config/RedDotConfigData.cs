using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace YIUIFramework
{
    /// <summary>
    /// 单个红点节点的静态配置数据（包含红点Key、所有父级红点Key列表及是否允许玩家手动开关提示）。
    /// </summary>
    [HideLabel]
    [HideReferenceObjectPicker]
    [Serializable]
    public class RedDotConfigData
    {
        [LabelText("Key")]
        [ShowInInspector]
        [OdinSerialize]
        public int Key { get; set; }

        [LabelText("所有父级列表")]
        [ShowInInspector]
        [OdinSerialize]
        public List<int> ParentList { get; set; } = new List<int>();

        [LabelText("是否允许开关提示")]
        [ShowInInspector]
        [OdinSerialize]
        public bool SwitchTips { get; set; } = true; //true = 玩家可开关 false = 不可开关 (永久提示)
    }
}