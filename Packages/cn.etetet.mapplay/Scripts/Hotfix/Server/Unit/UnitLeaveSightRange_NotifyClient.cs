namespace ET.Server
{
    /// <summary>
    /// AOI 视野离开事件处理器：当单位 B 离开玩家 A 的视野范围时，向玩家 A 的客户端发送 M2C_RemoveUnits 通知移除 B。
    /// </summary>
    // 离开视野
    [Event(SceneType.Map)]
    public class UnitLeaveSightRange_NotifyClient: AEvent<Scene, UnitLeaveSightRange>
    {
        protected override async ETTask Run(Scene scene, UnitLeaveSightRange args)
        {
            Unit a = args.A;
            Unit b = args.B;
            if (a.UnitType != UnitType.Player)
            {
                return;
            }
            
            M2C_RemoveUnits removeUnits = M2C_RemoveUnits.Create();
            removeUnits.Units.Add(b.Id);
            MapMessageHelper.NoticeClient(a, removeUnits, NoticeType.Self);
            
            await ETTask.CompletedTask;
        }
    }
}