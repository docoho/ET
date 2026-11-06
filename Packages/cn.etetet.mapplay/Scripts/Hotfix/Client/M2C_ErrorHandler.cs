namespace ET.Client
{
    /// <summary>
    /// 客户端处理地图服下发的错误提示消息（M2C_Error）：调用 TextHelper 输出格式化本地化错误文本。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_ErrorHandler: MessageHandler<Scene, M2C_Error>
    {
        protected override async ETTask Run(Scene root, M2C_Error message)
        {
            TextHelper.OutputText(root.Fiber(), message.Error, message.Values.ToArray());
            await ETTask.CompletedTask;
        }
    }
}
