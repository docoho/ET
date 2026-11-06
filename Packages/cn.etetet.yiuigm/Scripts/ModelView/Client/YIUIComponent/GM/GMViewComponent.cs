using System.Collections.Generic;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// GM调试主窗口视图实体，持有左侧分类滚动列表、右侧命令滚动列表及上次选中的分类索引缓存。
    /// </summary>
    public partial class GMViewComponent : Entity, IDynamicEvent<OnGMEventClose>
    {
        public bool                           Opened;
        public List<int>                      GMTypeData;
        public EntityRef<YIUILoopScrollChild> m_GMTypeLoop;
        public YIUILoopScrollChild            GMTypeLoop => m_GMTypeLoop;

        public EntityRef<YIUILoopScrollChild> m_GMCommandLoop;
        public YIUILoopScrollChild            GMCommandLoop => m_GMCommandLoop;

        public EntityRef<GMCommandComponent> m_CommandComponent;
        public GMCommandComponent            CommandComponent => m_CommandComponent;

        public IntPrefs m_GMTypeIndex = new("GMTypeIndex");
    }
}