namespace ET.Client
{
    /// <summary>
    /// 客户端接收断线重连帧同步房间通知（<see cref="G2C_Reconnect"/>）的消息处理器
    /// </summary>
    [MessageHandler(SceneType.LockStep)]
    public class G2C_ReconnectHandler: MessageHandler<Scene, G2C_Reconnect>
    {
        /// <summary>
        /// 收到重连通知后，切换到帧同步场景并恢复到服务端当前权威帧状态
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">断线重连数据消息</param>
        protected override async ETTask Run(Scene root, G2C_Reconnect message)
        {
            await LSSceneChangeHelper.SceneChangeToReconnect(root, message);
            await ETTask.CompletedTask;
        }
    }
}