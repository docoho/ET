using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET
{
    /// <summary>
    /// UnityBridge 在编辑器下新建空白或默认对象的场景（EditorSceneManager.NewScene）的命令处理器（SceneNew）。
    /// </summary>
    internal sealed class UnityBridgeSceneNewHandler : AUnityBridgeHandler<SceneNewRequest, SceneNewResponse>
    {
        protected override async ETTask<IResponse> Run(SceneNewRequest command)
        {
            await ETTask.CompletedTask;

            NewSceneSetup setup = string.Equals(command.Setup, "empty", StringComparison.OrdinalIgnoreCase)
                    ? NewSceneSetup.EmptyScene
                    : NewSceneSetup.DefaultGameObjects;

            UnityScene scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
            SceneNewResponse response = SceneNewResponse.Create();
            response.SceneName = scene.name;
            response.ScenePath = scene.path;
            response.Created = true;
            return response;
        }
    }
}
