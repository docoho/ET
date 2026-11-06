namespace ET.Client
{
    /// <summary>
    /// ObjectWait 协程等待信号：等待收到服务端推送的物品更新消息（M2C_UpdateItem）。
    /// </summary>
    public struct Wait_M2C_UpdateItem: IWaitType
    {
        /// <summary>收到的物品更新消息</summary>
        public M2C_UpdateItem M2C_UpdateItem;
        
        public int Error { get; set; }
    }
}