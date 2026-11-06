namespace ET.Server
{
    /// <summary>
    /// 服务端数值变更事件处理器：根据 NumericTypeConfig 配置的广播范围（NoticeType），向自身或视野内客户端广播 M2C_NumericChange。
    /// </summary>
    [Event(SceneType.Map)]
    public class NumericChangeEvent_Broadcast: AEvent<Scene, NumbericChange>
    {
        protected override async ETTask Run(Scene scene, NumbericChange a)
        {
            Unit unit = a.Unit;
            NumericTypeConfig numericTypeConfig = unit.Fiber().GetSingleton<NumericTypeConfigCategory>().Get((int)a.NumericType);
            M2C_NumericChange m2CNumericChange = M2C_NumericChange.Create();
            m2CNumericChange.NumericType = (int)a.NumericType;
            m2CNumericChange.UnitId = unit.Id;
            m2CNumericChange.Value = a.New;
            MapMessageHelper.NoticeClient(a.Unit, m2CNumericChange, numericTypeConfig.NoticeType);

            await ETTask.CompletedTask;
        }
    }
}
