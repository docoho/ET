using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 监听逻辑层派发的 Event_RedDot_Change 事件，将红点数量变更同步至视图层 RedDotMgr。
    /// </summary>
    [Event(SceneType.All)]
    public class On_Event_RedDot_Change_Handler : AEvent<Scene, Event_RedDot_Change>
    {
        protected override async ETTask Run(Scene currentScene, Event_RedDot_Change args)
        {
            RedDotMgr.Inst.SetCount(args.RedDotId, args.Count);
            await ETTask.CompletedTask;
        }
    }
}