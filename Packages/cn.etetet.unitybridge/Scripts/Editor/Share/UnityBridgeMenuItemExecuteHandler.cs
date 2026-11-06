using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 执行指定路径 Unity Editor 菜单项（EditorApplication.ExecuteMenuItem）的命令处理器（MenuItemExecute）。
    /// </summary>
    internal sealed class UnityBridgeMenuItemExecuteHandler : AUnityBridgeHandler<MenuItemExecuteRequest, MenuItemExecuteResponse>
    {
        protected override async ETTask<IResponse> Run(MenuItemExecuteRequest command)
        {
            await ETTask.CompletedTask;

            MenuItemExecuteResponse response = MenuItemExecuteResponse.Create();
            response.MenuPath = command.MenuPath;
            if (string.IsNullOrWhiteSpace(command.MenuPath))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "MenuPath is required";
                return response;
            }

            response.Executed = EditorApplication.ExecuteMenuItem(command.MenuPath);
            if (!response.Executed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = $"menu item not found or failed: {command.MenuPath}";
            }

            return response;
        }
    }
}
