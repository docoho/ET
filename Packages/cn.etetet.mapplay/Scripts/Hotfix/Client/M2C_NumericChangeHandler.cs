namespace ET.Client
{
    /// <summary>
    /// 客户端处理地图服广播的单位数值属性变更消息（M2C_NumericChange）。
    /// </summary>
    [MessageHandler(SceneType.Current)]
    public class M2C_NumericChangeHandler: MessageHandler<Scene, M2C_NumericChange>
    {
        protected override async ETTask Run(Scene currentScene, M2C_NumericChange message)
        {
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            unit.NumericComponent.Set(message.NumericType, message.Value);
            
            await ETTask.CompletedTask;
        }
    }
}
