namespace ET.Client
{
    /// <summary>
    /// 客户端当前地图子场景（CurrentScene）创建完成后的逻辑层事件回调：挂载客户端任务组件（QuestComponent）。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddQuestComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            scene.AddComponent<QuestComponent>();
            await ETTask.CompletedTask;
        }
    }
}
