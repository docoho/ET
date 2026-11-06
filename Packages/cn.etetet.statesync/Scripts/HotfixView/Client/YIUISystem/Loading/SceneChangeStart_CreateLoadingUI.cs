using System;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    /// <summary>
    /// 场景切换开始事件（<see cref="SceneChangeStart"/>）处理器，负责打开 Loading 界面并异步加载目标 Unity 地图场景资源
    /// </summary>
    [Event(SceneType.Client)]
    public class SceneChangeStart_CreateLoadingUI: AEvent<Scene, SceneChangeStart>
    {
        /// <summary>
        /// 若需要切换地图资源（<c>args.ChangeScene</c> 为 true），则打开 <see cref="LoadingPanelComponent"/> 并通过 <see cref="ResourcesLoaderComponent"/> 异步加载场景且实时更新加载进度
        /// </summary>
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                EntityRef<CurrentScenesComponent> currentScenesComponentRef = root.GetComponent<CurrentScenesComponent>();
                CurrentScenesComponent currentScenesComponent = currentScenesComponentRef;
                using var _ = await root.CoroutineLockComponent.Wait(CoroutineLockType.SceneChange, 0);

                root = rootRef;
                currentScenesComponent = currentScenesComponentRef;
                Scene currentScene = currentScenesComponent.Scene;
                EntityRef<Scene> currentSceneRef = currentScene;
                // 地图资源相同,则不创建Loading界面,也不需要重新加载地图
                if (args.ChangeScene)
                {
                    await root.YIUIRoot().OpenPanelAsync<LoadingPanelComponent>();

                    root = rootRef;
                    currentScenesComponent = currentScenesComponentRef;
                    currentScenesComponent.Progress = 0;

                    currentScene = currentSceneRef;
                    ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();

                    MapConfig mapConfig = root.Fiber().GetSingleton<MapConfigCategory>().GetByName(currentScene.Name.GetSceneConfigName());
                    await resourcesLoaderComponent.LoadSceneAsync(mapConfig.MapResName, LoadSceneMode.Single,
                        (progress) =>
                        {
                            CurrentScenesComponent currentScenes = currentScenesComponentRef;
                            currentScenes.Progress = (int)progress * 99f;
                        });
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
