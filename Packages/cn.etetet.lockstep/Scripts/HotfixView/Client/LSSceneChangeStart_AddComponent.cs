using UnityEngine.SceneManagement;

namespace ET.Client
{
    /// <summary>
    /// 帧同步场景切换开始事件（<see cref="LSSceneChangeStart"/>）表现层处理器，负责为房间挂载资源加载组件并准备场景与 UI 资源
    /// </summary>
    [Event(SceneType.LockStep)]
    public class LSSceneChangeStart_AddComponent: AEvent<Scene, LSSceneChangeStart>
    {
        /// <summary>
        /// 为新建的 <see cref="Room"/> 挂载 <see cref="ResourcesLoaderComponent"/>
        /// </summary>
        /// <param name="clientScene">客户端根场景</param>
        /// <param name="args">帧同步场景切换开始事件参数</param>
        protected override async ETTask Run(Scene clientScene, LSSceneChangeStart args)
        {
            Room room = clientScene.GetComponent<Room>();
            ResourcesLoaderComponent resourcesLoaderComponent = room.AddComponent<ResourcesLoaderComponent>();
            
            // 创建loading界面
            
            
            // 创建房间UI
            
            // 加载场景资源
            await ETTask.CompletedTask;
        }
    }
}