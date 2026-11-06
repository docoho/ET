using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取当前激活场景（SceneManager.GetActiveScene）的名称、路径、加载状态、未保存修改状态及根节点数量的命令处理器（SceneGetActive）。
    /// </summary>
    internal sealed class UnityBridgeSceneGetActiveHandler : AUnityBridgeHandler<SceneGetActiveRequest, SceneGetActiveResponse>
    {
        protected override async ETTask<IResponse> Run(SceneGetActiveRequest command)
        {
            await ETTask.CompletedTask;

            UnityScene scene = SceneManager.GetActiveScene();
            SceneGetActiveResponse response = SceneGetActiveResponse.Create();
            response.SceneName = scene.name;
            response.ScenePath = scene.path;
            response.IsLoaded = scene.isLoaded;
            response.IsDirty = scene.isDirty;
            response.RootCount = scene.rootCount;
            return response;
        }
    }
}
