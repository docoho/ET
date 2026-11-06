namespace ET.Server
{
    /// <summary>
    /// 服务端眩晕数值状态（NumericType.Stun）变更监听器：用于在进入或解除眩晕时暂停/恢复 AI 及打断移动。
    /// </summary>
    [NumericWatcher(SceneType.Map, NumericType.Stun)]
    public class NumericChange_Stun_StartOrStopAI: INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            //switch (args)
            //{
            //    case { Old: > 0, New: <= 0 }:
            //        unit.GetComponent<AIComponent>()?.Start();
            //        break;
            //    case { Old: <= 0, New: > 0 }:
            //        BuffHelper.RemoveBuffFlag(unit, BuffFlags.StunRemove);
            //        unit.Stop(0);
            //        unit.GetComponent<AIComponent>()?.Stop();
            //        break;
            //}
        }
    }
}