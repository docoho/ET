using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 水平无限循环滚动列表演示视图实体。
    /// </summary>
    public partial class LoopScrollHorizontalViewComponent : Entity
    {
        public EntityRef<YIUILoopScrollChild> m_Loop;
        public YIUILoopScrollChild Loop => m_Loop;
    }
}