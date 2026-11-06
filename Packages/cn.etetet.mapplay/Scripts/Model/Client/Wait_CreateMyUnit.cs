namespace ET.Client
{
    /// <summary>
    /// ObjectWait 协程等待信号：等待服务端下发创建本地玩家主角消息（M2C_CreateMyUnit）。
    /// </summary>
    public struct Wait_CreateMyUnit: IWaitType
    {
        public int Error
        {
            get;
            set;
        }

        /// <summary>收到的服务端创建主角消息</summary>
        public M2C_CreateMyUnit Message;
    }
}