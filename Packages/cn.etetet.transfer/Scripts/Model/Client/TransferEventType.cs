namespace ET.Client
{
    /// <summary>
    /// 客户端场景切换开始事件（表现层可订阅此事件打开 Loading 界面并加载 Unity 场景资源）。
    /// </summary>
    public struct SceneChangeStart
    {
        /// <summary>
        /// 是否真正切换了地图资源（若为 false 表示同地图切分线）。
        /// </summary>
        public bool ChangeScene;
    }

    /// <summary>
    /// 客户端场景切换完成事件（表现层可订阅此事件关闭 Loading 界面并初始化场景表现）。
    /// </summary>
    public struct SceneChangeFinish
    {
    }
}
