namespace ET.Client
{
    /// <summary>
    /// 客户端接收服务端 Buff 移除通知（M2C_BuffRemove）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Current)]
    public class M2C_BuffRemoveHandler: MessageHandler<Scene, M2C_BuffRemove>
    {
        /// <summary>
        /// 移除目标单位身上的指定 Buff。
        /// </summary>
        protected override async ETTask Run(Scene currentScene, M2C_BuffRemove message)
        {
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            BuffHelper.RemoveBuff(unit, message.BuffId, (BuffFlags)message.RemoveType);
            await ETTask.CompletedTask;
        }
    }
}
