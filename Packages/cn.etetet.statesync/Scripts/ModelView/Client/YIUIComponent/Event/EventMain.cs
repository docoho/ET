namespace ET.Client
{
    /// <summary>
    /// 请求将单位头顶血条挂载到 HUD 面板显示区域的 UI 动态事件参数
    /// </summary>
    public struct EventMain_ShowHPView
    {
        /// <summary>
        /// 需要显示的 2D 头顶血条组件引用
        /// </summary>
        public EntityRef<HPViewComponent> HPView;
    }
}