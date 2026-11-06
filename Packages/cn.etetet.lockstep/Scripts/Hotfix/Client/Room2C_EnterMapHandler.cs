namespace ET.Client
{
    /// <summary>
    /// 客户端接收房间正式开局通知（<see cref="Room2C_Start"/>）的处理器
    /// </summary>
    [MessageHandler(SceneType.LockStep)]
    public class Room2C_EnterMapHandler: MessageHandler<Scene, Room2C_Start>
    {
        /// <summary>
        /// 唤醒正在等待 <see cref="Wait_Room2C_Start"/> 信号的场景切换协程，携带初始单位列表与开局时间戳启动帧同步世界
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">房间开局消息</param>
        protected override async ETTask Run(Scene root, Room2C_Start message)
        {
            root.GetComponent<ObjectWait>().Notify(new Wait_Room2C_Start() {Message = message});
            await ETTask.CompletedTask;
        }
    }
}