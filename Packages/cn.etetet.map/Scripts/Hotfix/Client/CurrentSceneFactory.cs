namespace ET.Client
{
    /// <summary>
    /// 客户端当前地图子场景（<see cref="SceneType.Current"/>）创建工厂。
    /// </summary>
    public static class CurrentSceneFactory
    {
        /// <summary>
        /// 在 <paramref name="currentScenesComponent"/> 下创建新的客户端当前场景，挂载无序邮箱组件并发布 <see cref="AfterCreateCurrentScene"/> 事件。
        /// </summary>
        public static Scene Create(long id, string name, CurrentScenesComponent currentScenesComponent)
        {
            Scene currentScene = EntitySceneFactory.CreateScene(currentScenesComponent, id, SceneType.Current, name);
            currentScene.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            currentScenesComponent.Scene = currentScene;
            
            EventSystem.Instance.Publish(currentScene, new AfterCreateCurrentScene());
            return currentScene;
        }
    }
}
