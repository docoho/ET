namespace ET.Server
{
    /// <summary>
    /// 服务端处理客户端主动停止移动请求（C2M_Stop）的位置消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_StopHandler: MessageLocationHandler<Unit, C2M_Stop>
    {
        /// <summary>
        /// 停止该单位当前的移动并广播停止坐标。
        /// </summary>
        protected override async ETTask Run(Unit unit, C2M_Stop message)
        {
            unit.Stop(1);
            await ETTask.CompletedTask;
        }
    }
}