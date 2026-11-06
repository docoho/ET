namespace ET.Client
{
    /// <summary>
    /// 客户端根 Scene 创建完成事件。
    /// </summary>
    public struct AfterCreateClientScene
    {
    }

    /// <summary>
    /// 客户端当前地图子场景（<see cref="SceneType.Current"/>）创建完成事件。
    /// </summary>
    public struct AfterCreateCurrentScene
    {
    }

    /// <summary>
    /// 客户端应用启动初始化事件。
    /// </summary>
    public struct AppStartInit
    {
    }

    /// <summary>
    /// 客户端应用启动初始化完成事件。
    /// </summary>
    public struct AppStartInitFinish
    {
    }

    /// <summary>
    /// 客户端普通单位创建完成事件（表现层可订阅以加载模型 GameObject）。
    /// </summary>
    public struct AfterUnitCreate
    {
        /// <summary>
        /// 新创建的单位实体引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }

    /// <summary>
    /// 客户端主角单位创建完成事件（表现层可订阅以绑定摄像机与主控输入）。
    /// </summary>
    public struct AfterMyUnitCreate
    {
        /// <summary>
        /// 新创建的主角单位实体引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }
}
