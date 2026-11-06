namespace ET.Client
{
    /// <summary>
    /// 客户端等待单位停止移动的 <see cref="ObjectWait"/> 信号结构体。
    /// </summary>
    public struct Wait_UnitStop: IWaitType
    {
        /// <summary>
        /// 停止原因或错误码（0 表示正常到达终点）。
        /// </summary>
        public int Error
        {
            get;
            set;
        }
    }
}