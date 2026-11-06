namespace ET.Client
{
    /// <summary>
    /// 客户端接收服务端 Buff 状态更新通知（M2C_BuffUpdate）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Current)]
    public class M2C_BuffUpdateHandler: MessageHandler<Scene, M2C_BuffUpdate>
    {
        /// <summary>
        /// 同步更新目标 Buff 的过期时间、Tick 时间与层数。
        /// </summary>
        protected override async ETTask Run(Scene currentScene, M2C_BuffUpdate message)
        {
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            Buff buff = unit.GetComponent<BuffComponent>().GetChild<Buff>(message.BuffId);
            BuffHelper.UpdateBuff(buff, message.ExpireTime, message.TickTime, message.Stack);
            await ETTask.CompletedTask;
        }
    }
}
