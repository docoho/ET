namespace ET.Client
{
    /// <summary>
    /// 新手 03 的 Demo 启动入口：Unity Editor 点击 Play 后自动演示一次，无需登录。
    /// 使用现有 EntryEvent1，不修改游戏启动顺序，也不保留演示组件。
    /// </summary>
    [Event(SceneType.StateSync)]
    public class EntryEvent1_PracticeEntity : AEvent<Scene, EntryEvent1>
    {
        protected override ETTask Run(Scene root, EntryEvent1 args) 
        {
#if UNITY_EDITOR
            // 教学输入：期望依次看到 Awake=10、Increase=13、Destroy=13、查询=null。
            const int initialValue = 10;
            const int increment = 3;
            PracticeEntityHelper.Run(root, initialValue, increment);
#endif
            return ETTask.CompletedTask;
        }
    }
}
