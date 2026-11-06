using System.Collections.Generic;
using TrueSync;

namespace ET.Server
{
    /// <summary>
    /// 客户端通知房间场景加载完成（<see cref="C2Room_ChangeSceneFinish"/>）的消息处理器
    /// </summary>
    [MessageHandler(SceneType.RoomRoot)]
    [FriendOf(typeof (RoomServerComponent))]
    public class C2Room_ChangeSceneFinishHandler: MessageHandler<Scene, C2Room_ChangeSceneFinish>
    {
        /// <summary>
        /// 记录玩家加载进度为 100%；当房间内所有玩家均加载完毕后，初始化各单位初始定点数坐标、挂载 <see cref="LSServerUpdater"/> 并广播 <see cref="Room2C_Start"/> 正式开局
        /// </summary>
        /// <param name="root">房间纤程根场景</param>
        /// <param name="message">客户端场景切换完成通知</param>
        protected override async ETTask Run(Scene root, C2Room_ChangeSceneFinish message)
        {
            Room room = root.GetComponent<Room>();
            RoomServerComponent roomServerComponent = room.GetComponent<RoomServerComponent>();
            RoomPlayer roomPlayer = room.GetComponent<RoomServerComponent>().GetChild<RoomPlayer>(message.PlayerId);
            roomPlayer.Progress = 100;
            
            if (!roomServerComponent.IsAllPlayerProgress100())
            {
                return;
            }

            EntityRef<Room> roomRef = room;
            await room.Fiber.Root.TimerComponent.WaitAsync(1000);

            room = roomRef;
            Room2C_Start room2CStart = Room2C_Start.Create();
            room2CStart.StartTime = room.GetSingleton<TimeInfo>().ServerNow();

            roomServerComponent = room.GetComponent<RoomServerComponent>();
            foreach (RoomPlayer rp in roomServerComponent.Children.Values)
            {
                LockStepUnitInfo lockStepUnitInfo = LockStepUnitInfo.Create();
                lockStepUnitInfo.PlayerId = rp.Id;
                lockStepUnitInfo.Position = new TSVector(20, 0, -10);
                lockStepUnitInfo.Rotation = TSQuaternion.identity;
                room2CStart.UnitInfo.Add(lockStepUnitInfo);
            }

            room = roomRef;
            room.Init(room2CStart.UnitInfo, room2CStart.StartTime);

            room.AddComponent<LSServerUpdater>();

            RoomMessageHelper.BroadCast(room, room2CStart);
        }
    }
}
