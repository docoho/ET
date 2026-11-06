using System;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端录像回放更新器系统，负责按录像帧数据驱动世界演进并支持倍速播放切换
    /// </summary>
    [EntitySystemOf(typeof(LSReplayUpdater))]
    [FriendOf(typeof(LSReplayUpdater))]
    public static partial class LSReplayUpdaterSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSReplayUpdater self)
        {

        }
        
        /// <summary>
        /// 每帧根据固定时间计数器从 <see cref="Replay.FrameInputs"/> 中取出对应帧输入驱动回放世界更新
        /// </summary>
        /// <param name="self">录像回放更新器实例</param>
        [EntitySystem]
        private static void Update(this LSReplayUpdater self)
        {
            Room room = self.GetParent<Room>();
            Fiber fiber = self.Fiber();
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();

            int i = 0;
            while (true)
            {
                if (room.AuthorityFrame + 1 >= room.Replay.FrameInputs.Count)
                {
                    break;
                }

                if (timeNow < room.FixedTimeCounter.FrameTime(room.AuthorityFrame + 1))
                {
                    break;
                }

                ++room.AuthorityFrame;

                OneFrameInputs oneFrameInputs = room.Replay.FrameInputs[room.AuthorityFrame];

                room.Update(oneFrameInputs);
                room.SpeedMultiply = ++i;

                long timeNow2 = self.GetSingleton<TimeInfo>().ServerNow();
                if (timeNow2 - timeNow > 5)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 循环切换录像回放倍速（1x -> 2x -> 4x -> 8x -> 1x），并动态调整 <see cref="FixedTimeCounter"/> 的帧时间间隔
        /// </summary>
        /// <param name="self">录像回放更新器实例</param>
        public static void ChangeReplaySpeed(this LSReplayUpdater self)
        {
            Room room = self.Room();
            LSReplayUpdater lsReplayUpdater = room.GetComponent<LSReplayUpdater>();
            if (lsReplayUpdater.ReplaySpeed == 8)
            {
                lsReplayUpdater.ReplaySpeed = 1;
            }
            else
            {
                lsReplayUpdater.ReplaySpeed *= 2;
            }

            int updateInterval = LSConstValue.UpdateInterval / lsReplayUpdater.ReplaySpeed;
            room.FixedTimeCounter.ChangeInterval(updateInterval, room.AuthorityFrame);
        }
    }
}