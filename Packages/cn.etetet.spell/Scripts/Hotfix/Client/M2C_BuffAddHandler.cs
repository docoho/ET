namespace ET.Client
{
    /// <summary>
    /// 客户端接收服务端 Buff 添加通知（M2C_BuffAdd）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Current)]
    public class M2C_BuffAddHandler: MessageHandler<Scene, M2C_BuffAdd>
    {
        /// <summary>
        /// 在目标单位身上创建客户端 Buff 实例。
        /// </summary>
        protected override async ETTask Run(Scene currentScene, M2C_BuffAdd message)
        {
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            BuffHelper.CreateBuff(unit, message);
            await ETTask.CompletedTask;
        }
    }
}
