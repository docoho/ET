namespace ET.Client
{
    /// <summary>
    /// 客户端接收服务端技能冷却时间更新通知（M2C_UpdateCD）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Current)]
    public class M2C_UpdateCDHandler : MessageHandler<Scene, M2C_UpdateCD>
    {
        /// <summary>
        /// 更新客户端主角或指定单位 <see cref="SpellComponent"/> 中的技能 CD 时间戳。
        /// </summary>
        protected override async ETTask Run(Scene currentScene, M2C_UpdateCD message)
        {
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            if (unit == null)
            {
                return;
            }

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.UpdateCD(message.SpellConfigId, message.Time);
            await ETTask.CompletedTask;
        }
    }
}
