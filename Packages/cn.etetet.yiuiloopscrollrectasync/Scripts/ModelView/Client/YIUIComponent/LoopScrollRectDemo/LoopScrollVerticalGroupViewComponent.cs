using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 纵向多列网格（Group）无限循环滚动列表演示视图实体。
    /// </summary>
    public partial class LoopScrollVerticalGroupViewComponent : Entity
    {
        public EntityRef<YIUILoopScrollChild> m_Loop;
        public YIUILoopScrollChild Loop => m_Loop;
    }
}