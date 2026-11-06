using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 网关转发的玩家断线重连帧同步房间请求（<see cref="G2Room_Reconnect"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.RoomRoot)]
    public class G2Room_ReconnectHandler: MessageHandler<Scene, G2Room_Reconnect, Room2G_Reconnect>
    {
        /// <summary>
        /// 返回当前房间的开局时间戳、最新权威帧号以及所有玩家的单位信息，供客户端重连追帧
        /// </summary>
        /// <param name="root">房间根场景</param>
        /// <param name="request">重连请求消息</param>
        /// <param name="response">重连响应消息</param>
        protected override async ETTask Run(Scene root, G2Room_Reconnect request, Room2G_Reconnect response)
        {
            Room room = root.GetComponent<Room>();
            response.StartTime = room.StartTime;
            LSUnitComponent lsUnitComponent = room.LSWorld.GetComponent<LSUnitComponent>();
            foreach (long playerId in room.PlayerIds)
            {
                LSUnit lsUnit = lsUnitComponent.GetChild<LSUnit>(playerId);
                LockStepUnitInfo lockStepUnitInfo = LockStepUnitInfo.Create();
                lockStepUnitInfo.PlayerId = playerId;
                lockStepUnitInfo.Position = lsUnit.Position;
                lockStepUnitInfo.Rotation = lsUnit.Rotation;
                response.UnitInfos.Add(lockStepUnitInfo);    
            }

            response.Frame = room.AuthorityFrame;
            await ETTask.CompletedTask;
        }
    }
}