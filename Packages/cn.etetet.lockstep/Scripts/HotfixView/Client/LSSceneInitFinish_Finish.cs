namespace ET.Client
{
    /// <summary>
    /// 帧同步场景初始化完成事件（<see cref="LSSceneInitFinish"/>）表现层处理器，负责初始化所有单位视图、跟随相机与玩家操作输入组件
    /// </summary>
    [Event(SceneType.LockStep)]
    public class LSSceneInitFinish_Finish: AEvent<Scene, LSSceneInitFinish>
    {
        /// <summary>
        /// 挂载并初始化 <see cref="LSUnitViewComponent"/>、<see cref="LSCameraComponent"/>，非录像模式下额外挂载 <see cref="LSOperaComponent"/>
        /// </summary>
        /// <param name="clientScene">客户端根场景</param>
        /// <param name="args">帧同步场景初始化完成事件参数</param>
        protected override async ETTask Run(Scene clientScene, LSSceneInitFinish args)
        {
            Room room = clientScene.GetComponent<Room>();
            
            await room.AddComponent<LSUnitViewComponent>().InitAsync();
            
            room.AddComponent<LSCameraComponent>();

            if (!room.IsReplay)
            {
                room.AddComponent<LSOperaComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}