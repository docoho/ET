using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET
{
    /// <summary>
    /// UnityBridge 在编辑器下打开指定路径场景（支持 Single 或 Additive 模式）的命令处理器（SceneLoad）。
    /// </summary>
    internal sealed class UnityBridgeSceneLoadHandler : AUnityBridgeHandler<SceneLoadRequest, SceneLoadResponse>
    {
        protected override async ETTask<IResponse> Run(SceneLoadRequest command)
        {
            await ETTask.CompletedTask;
            if (string.IsNullOrWhiteSpace(command.ScenePath))
            {
                throw new ArgumentException("scene path is required");
            }

            OpenSceneMode mode = string.Equals(command.Mode, "additive", StringComparison.OrdinalIgnoreCase)
                    ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

            UnityScene scene = EditorSceneManager.OpenScene(command.ScenePath.Trim(), mode);
            SceneLoadResponse response = SceneLoadResponse.Create();
            response.SceneName = scene.name;
            response.ScenePath = command.ScenePath.Trim();
            response.Loaded = scene.isLoaded;
            return response;
        }
    }
}
