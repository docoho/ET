using System;
using System.IO;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端逻辑帧更新器系统，负责客户端本地预测帧推进（最多超前权威帧 5 帧）、本地操作采样上报及追帧限流
    /// </summary>
    [EntitySystemOf(typeof(LSClientUpdater))]
    [FriendOf(typeof (LSClientUpdater))]
    public static partial class LSClientUpdaterSystem
    {
        /// <summary>
        /// 初始化客户端帧同步更新器，缓存本地玩家 ID
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSClientUpdater self)
        {
            Room room = self.GetParent<Room>();
            self.MyId = room.Root().GetComponent<PlayerComponent>().MyId;
        }
        
        /// <summary>
        /// 每帧检测是否到达下一预测帧时间点，采集本地输入推进预测模拟并向服务端发送 <see cref="FrameMessage"/>（单次渲染帧最多耗时 5ms 以防卡顿）
        /// </summary>
        /// <param name="self">客户端帧同步更新器实例</param>
        [EntitySystem]
        private static void Update(this LSClientUpdater self)
        {
            Room room = self.GetParent<Room>();
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();
            Scene root = room.Root();

            int i = 0;
            while (true)
            {
                if (timeNow < room.FixedTimeCounter.FrameTime(room.PredictionFrame + 1))
                {
                    return;
                }

                // 最多只预测5帧
                if (room.PredictionFrame - room.AuthorityFrame > 5)
                {
                    return;
                }

                ++room.PredictionFrame;
                OneFrameInputs oneFrameInputs = self.GetOneFrameMessages(room.PredictionFrame);
                
                room.Update(oneFrameInputs);
                room.SendHash(room.PredictionFrame);
                
                room.SpeedMultiply = ++i;

                FrameMessage frameMessage = FrameMessage.Create();
                frameMessage.Frame = room.PredictionFrame;
                frameMessage.Input = self.Input;
                root.GetComponent<ClientSenderComponent>().Send(frameMessage);
                
                long timeNow2 = self.GetSingleton<TimeInfo>().ServerNow();
                if (timeNow2 - timeNow > 5)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 构造指定预测帧的全员输入：沿用最新权威帧中其他玩家的输入，并写入本地玩家当前采集到的操作输入
        /// </summary>
        /// <param name="self">客户端帧同步更新器实例</param>
        /// <param name="frame">目标帧号</param>
        /// <returns>预测帧输入数据</returns>
        private static OneFrameInputs GetOneFrameMessages(this LSClientUpdater self, int frame)
        {
            Room room = self.GetParent<Room>();
            FrameBuffer frameBuffer = room.FrameBuffer;
            
            if (frame <= room.AuthorityFrame)
            {
                return frameBuffer.FrameInputs(frame);
            }
            
            // predict
            OneFrameInputs predictionFrame = frameBuffer.FrameInputs(frame);
            
            frameBuffer.MoveForward(frame);
            if (frameBuffer.CheckFrame(room.AuthorityFrame))
            {
                OneFrameInputs authorityFrame = frameBuffer.FrameInputs(room.AuthorityFrame);
                authorityFrame.CopyTo(predictionFrame);
            }
            predictionFrame.Inputs[self.MyId] = self.Input;
            
            return predictionFrame;
        }
    }
}