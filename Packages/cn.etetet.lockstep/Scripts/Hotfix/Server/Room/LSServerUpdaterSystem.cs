using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 帧同步服务端权威帧更新器系统，按固定帧率推进房间权威帧、收集并补全各玩家帧输入、向全员广播并推进服务端权威世界模拟
    /// </summary>
    [EntitySystemOf(typeof(LSServerUpdater))]
    [FriendOf(typeof(LSServerUpdater))]
    public static partial class LSServerUpdaterSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSServerUpdater self)
        {

        }
        
        /// <summary>
        /// 每帧检测固定时间步长是否到达下一权威帧时间点；若到达则打包本帧所有玩家输入、全房间广播并驱动服务端 <see cref="Room.Update"/>
        /// </summary>
        /// <param name="self">服务端帧同步更新器实例</param>
        [EntitySystem]
        private static void Update(this LSServerUpdater self)
        {
            Room room = self.GetParent<Room>();
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();


            int frame = room.AuthorityFrame + 1;
            if (timeNow < room.FixedTimeCounter.FrameTime(frame))
            {
                return;
            }

            OneFrameInputs oneFrameInputs = self.GetOneFrameMessage(frame);
            ++room.AuthorityFrame;

            OneFrameInputs sendInput = OneFrameInputs.Create();
            oneFrameInputs.CopyTo(sendInput);

            RoomMessageHelper.BroadCast(room, sendInput);

            room.Update(oneFrameInputs);
        }

        /// <summary>
        /// 获取并补全指定逻辑帧的全员输入：若某玩家该帧输入未及时送达，则自动沿用其上一帧输入（或默认空输入）以保证权威帧按时推进
        /// </summary>
        /// <param name="self">服务端帧同步更新器实例</param>
        /// <param name="frame">目标权威帧号</param>
        /// <returns>包含房间所有玩家输入的单帧输入集合</returns>
        private static OneFrameInputs GetOneFrameMessage(this LSServerUpdater self, int frame)
        {
            Room room = self.GetParent<Room>();
            FrameBuffer frameBuffer = room.FrameBuffer;
            OneFrameInputs oneFrameInputs = frameBuffer.FrameInputs(frame);
            frameBuffer.MoveForward(frame);

            if (oneFrameInputs.Inputs.Count == LSConstValue.MatchCount)
            {
                return oneFrameInputs;
            }

            OneFrameInputs preFrameInputs = null;
            if (frameBuffer.CheckFrame(frame - 1))
            {
                preFrameInputs = frameBuffer.FrameInputs(frame - 1);
            }

            // 有人输入的消息没过来，给他使用上一帧的操作
            foreach (long playerId in room.PlayerIds)
            {
                if (oneFrameInputs.Inputs.ContainsKey(playerId))
                {
                    continue;
                }

                if (preFrameInputs != null && preFrameInputs.Inputs.TryGetValue(playerId, out LSInput input))
                {
                    // 使用上一帧的输入
                    oneFrameInputs.Inputs[playerId] = input;
                }
                else
                {
                    oneFrameInputs.Inputs[playerId] = new LSInput();
                }
            }

            return oneFrameInputs;
        }
    }
}