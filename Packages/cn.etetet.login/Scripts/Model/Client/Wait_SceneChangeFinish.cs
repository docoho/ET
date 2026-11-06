namespace ET.Client
{
    /// <summary>
    /// ObjectWait 协程等待信号：等待客户端地图场景切换加载完成。
    /// </summary>
    public struct Wait_SceneChangeFinish: IWaitType
    {
        /// <summary>等待结果错误码</summary>
        public int Error
        {
            get;
            set;
        }
    }
}