using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 显示3D模型的Demo视图组件，持有YIUI3DDisplayChild子实体引用。
    /// </summary>
    public partial class ModelDisplayDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        /// <summary>3D展示子实体弱引用。</summary>
        public EntityRef<YIUI3DDisplayChild> m_Display;
        /// <summary>获取3D展示子实体。</summary>
        public YIUI3DDisplayChild            Display => m_Display;
    }
}