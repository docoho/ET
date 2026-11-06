namespace ET.Client
{
    /// <summary>
    /// 挂载在YIUIWindowComponent上的Tips子窗口标记组件，用于在窗口关闭时触发对象池回收。
    /// </summary>
    [ComponentOf(typeof(YIUIWindowComponent))]
    public partial class TipsViewComponent : Entity, IAwake, IDestroy, IYIUIWindowClose
    {
        public bool m_IsFromTips;
    }

    /// <summary>
    /// 通用弹窗view关闭事件
    /// </summary>
    public struct EventPutTipsView
    {
        public EntityRef<Entity> View;
        public bool   Destroy;
    }
}