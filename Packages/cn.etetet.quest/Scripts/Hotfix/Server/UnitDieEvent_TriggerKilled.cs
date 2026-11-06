namespace ET.Server
{
    /// <summary>
    /// 服务端单位死亡事件（UnitDie）处理器：通知击杀者的任务系统触发怪物击杀任务进度更新。
    /// </summary>
    [Event(SceneType.Map)]
    public class UnitDieEvent_TriggerKilled: AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            QuestEventHelper.OnMonsterKilled(a.Unit, a.Target.Entity.Id, 1);
            await ETTask.CompletedTask;
        }
    }
}