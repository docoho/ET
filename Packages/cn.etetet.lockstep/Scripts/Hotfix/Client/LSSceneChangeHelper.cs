namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端场景切换辅助类，提供正常匹配入场、录像回放加载与断线重连入场三种场景初始化协程
    /// </summary>
    public static partial class LSSceneChangeHelper
    {
        // 场景切换协程
        /// <summary>
        /// 正常匹配进入帧同步对局场景：创建 <see cref="Room"/>、通知表现层加载资源、上报加载完成并等待 <see cref="Room2C_Start"/> 消息后启动 <see cref="LSClientUpdater"/>
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="sceneName">目标地图场景名称</param>
        /// <param name="sceneInstanceId">房间实例 ID</param>
        public static async ETTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            EntityRef<Scene> rootRef = root;
            root.RemoveComponent<Room>();

            Room room = root.AddComponentWithId<Room>(sceneInstanceId);
            room.Name = sceneName;

            EntityRef<Room> roomRef = room;

            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(root, new LSSceneChangeStart() {Room = room});

            root = rootRef;
            root.GetComponent<ClientSenderComponent>().Send(C2Room_ChangeSceneFinish.Create());
            
            // 等待Room2C_EnterMap消息
            Wait_Room2C_Start waitRoom2CStart = await root.GetComponent<ObjectWait>().Wait<Wait_Room2C_Start>();

            room = roomRef;
            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(waitRoom2CStart.Message.UnitInfo, waitRoom2CStart.Message.StartTime);
            
            room.AddComponent<LSClientUpdater>();

            root = rootRef;
            // 这个事件中可以订阅取消loading
            EventSystem.Instance.Publish(root, new LSSceneInitFinish());
        }
        
        // 场景切换协程
        /// <summary>
        /// 切换进入帧同步录像回放场景：使用 <see cref="Replay"/> 数据初始化房间与 <see cref="LSWorld"/>，并挂载 <see cref="LSReplayUpdater"/>
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="replay">反序列化后的录像数据对象</param>
        public static async ETTask SceneChangeToReplay(Scene root, Replay replay)
        {
            root.RemoveComponent<Room>();

            Room room = root.AddComponent<Room>();
            room.Name = "Map1";
            room.IsReplay = true;
            room.Replay = replay;
            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(replay.UnitInfos, root.GetSingleton<TimeInfo>().ServerNow());

            EntityRef<Scene> rootRef = root;
            EntityRef<Room> roomRef = room;
            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(root, new LSSceneChangeStart() {Room = room});

            room = roomRef;
            room.AddComponent<LSReplayUpdater>();
            // 这个事件中可以订阅取消loading
            root = rootRef;
            EventSystem.Instance.Publish(root, new LSSceneInitFinish());
        }
        
        // 场景切换协程
        /// <summary>
        /// 断线重连进入帧同步场景：根据重连消息中的开局时间、单位快照及最新权威帧号恢复房间并启动 <see cref="LSClientUpdater"/>
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">服务端下发的重连状态数据</param>
        public static async ETTask SceneChangeToReconnect(Scene root, G2C_Reconnect message)
        {
            root.RemoveComponent<Room>();

            Room room = root.AddComponent<Room>();
            room.Name = "Map1";
            
            room.LSWorld = new LSWorld(SceneType.LockStepClient);
            room.Init(message.UnitInfos, message.StartTime, message.Frame);

            EntityRef<Room> roomRef = room;
            EntityRef<Scene> rootRef = root;
            // 等待表现层订阅的事件完成
            await EventSystem.Instance.PublishAsync(root, new LSSceneChangeStart() {Room = room});

            room = roomRef;
            room.AddComponent<LSClientUpdater>();
            // 这个事件中可以订阅取消loading
            root = rootRef;
            EventSystem.Instance.Publish(root, new LSSceneInitFinish());
        }
    }
}