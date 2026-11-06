namespace ET.Client
{
    /// <summary>
    /// 客户端表现层处理地图服下发的错误提示消息（M2C_Error）：根据错误码查询 TextConfig 中文模板，弹出红色飘字提示（TipsTextViewComponent）。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_ErrorViewHandler : MessageHandler<Scene, M2C_Error>
    {
        protected override async ETTask Run(Scene root, M2C_Error message)
        {
            TipsHelper.OpenSync<TipsTextViewComponent>(root, $"<color=red>{OutputText(root.Fiber(), message.Error, message.Values.ToArray())}</color>");
            await ETTask.CompletedTask;
        }

        private static string OutputText(Fiber fiber, int key, params string[] args)
        {
            return string.Format(GetText(fiber, key), args);
        }

        private static string GetText(Fiber fiber, int key)
        {
            return fiber.GetSingleton<TextConfigCategory>().Get(key).CN;
        }
    }
}
