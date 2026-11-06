using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 房间接收客户端上报的单帧操作输入消息（<see cref="FrameMessage"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.RoomRoot)]
    public class FrameMessageHandler: MessageHandler<Scene, FrameMessage>
    {
        /// <summary>
        /// 处理客户端帧输入：每秒下发一次客户端时间微调指令 <see cref="Room2C_AdjustUpdateTime"/>，过滤过期或超前过多的帧消息，并将有效输入写入 <see cref="FrameBuffer"/>
        /// </summary>
        /// <param name="root">房间纤程根场景</param>
        /// <param name="message">客户端上报的单帧操作输入消息</param>
        protected override async ETTask Run(Scene root, FrameMessage message)
        {
            using FrameMessage _ = message;  // 让消息回到池中
            
            Room room = root.GetComponent<Room>();
            FrameBuffer frameBuffer = room.FrameBuffer;
            if (message.Frame % (1000 / LSConstValue.UpdateInterval) == 0)
            {
                long nowFrameTime = room.FixedTimeCounter.FrameTime(message.Frame);
                int diffTime = (int)(nowFrameTime - root.GetSingleton<TimeInfo>().ServerNow());

                Room2C_AdjustUpdateTime room2CAdjustUpdateTime = Room2C_AdjustUpdateTime.Create();
                room2CAdjustUpdateTime.DiffTime = diffTime;
                room.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(message.PlayerId, room2CAdjustUpdateTime);
            }

            if (message.Frame < room.AuthorityFrame)  // 小于AuthorityFrame，丢弃
            {
                Log.Warning($"FrameMessage < AuthorityFrame discard: {message}");
                return;
            }

            if (message.Frame > room.AuthorityFrame + 10)  // 大于AuthorityFrame + 10，丢弃
            {
                Log.Warning($"FrameMessage > AuthorityFrame + 10 discard: {message}");
                return;
            }
            
            OneFrameInputs oneFrameInputs = frameBuffer.FrameInputs(message.Frame);
            if (oneFrameInputs == null)
            {
                Log.Error($"FrameMessageHandler get frame is null: {message.Frame}, max frame: {frameBuffer.MaxFrame}");
                return;
            }
            oneFrameInputs.Inputs[message.PlayerId] = message.Input;

            await ETTask.CompletedTask;
        }
    }
}