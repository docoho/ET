namespace ET.Client
{
    /// <summary>
    /// 等待服务端房间广播正式开局消息（<see cref="Room2C_Start"/>）的异步等待信号
    /// </summary>
    public struct Wait_Room2C_Start: IWaitType
    {
        /// <summary>
        /// 等待结果错误码
        /// </summary>
        public int Error { get; set; }

        /// <summary>
        /// 收到的房间开局消息
        /// </summary>
        public Room2C_Start Message;
    }
    
    /// <summary>
    /// 等待客户端场景切换完成的异步等待信号
    /// </summary>
    public struct Wait_SceneChangeFinish: IWaitType
    {
        /// <summary>
        /// 等待结果错误码
        /// </summary>
        public int Error
        {
            get;
            set;
        }
    }
}