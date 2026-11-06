namespace ET.Client
{
    /// <summary>
    /// 客户端接收匹配成功通知（<see cref="Match2G_NotifyMatchSuccess"/>）的消息处理器
    /// </summary>
    [MessageHandler(SceneType.LockStep)]
    public class Match2G_NotifyMatchSuccessHandler: MessageHandler<Scene, Match2G_NotifyMatchSuccess>
    {
        /// <summary>
        /// 收到匹配成功通知后，触发客户端切换进入帧同步对局场景
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">匹配成功通知消息（包含房间 ActorId）</param>
        protected override async ETTask Run(Scene root, Match2G_NotifyMatchSuccess message)
        {
            await LSSceneChangeHelper.SceneChangeTo(root, "Map1", message.ActorId.InstanceId);
        }
    }
}