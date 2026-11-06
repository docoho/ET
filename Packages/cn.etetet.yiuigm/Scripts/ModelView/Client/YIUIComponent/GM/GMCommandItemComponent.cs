using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// GM命令列表单项UI实体，持有命令元数据及参数滚动列表子实体引用。
    /// </summary>
    public partial class GMCommandItemComponent : Entity
    {
        public EntityRef<GMCommandComponent> m_CommandComponent;
        public GMCommandComponent            CommandComponent => m_CommandComponent;
        public GMCommandInfo                 Info;

        public EntityRef<YIUILoopScrollChild> m_GMParamLoop;
        public YIUILoopScrollChild            GMParamLoop => m_GMParamLoop;
    }
}