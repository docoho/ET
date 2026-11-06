namespace ET.Client
{
    /// <summary>
    /// 客户端接收服务端下发的帧率时间微调指令（<see cref="Room2C_AdjustUpdateTime"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.LockStep)]
    public class Room2C_AdjustUpdateTimeHandler: MessageHandler<Scene, Room2C_AdjustUpdateTime>
    {
        /// <summary>
        /// 根据客户端上报帧与服务端当前帧的时间偏差（DiffTime），在 [40ms, 66ms] 范围内动态微调本地 <see cref="FixedTimeCounter"/> 的单帧间隔，使客户端预测超前量保持稳定
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">时间调整消息</param>
        protected override async ETTask Run(Scene root, Room2C_AdjustUpdateTime message)
        {
            Room room = root.GetComponent<Room>();
            int newInterval = (1000 + (message.DiffTime - LSConstValue.UpdateInterval)) * LSConstValue.UpdateInterval / 1000;

            if (newInterval < 40)
            {
                newInterval = 40;
            }

            if (newInterval > 66)
            {
                newInterval = 66;
            }
            
            room.FixedTimeCounter.ChangeInterval(newInterval, room.PredictionFrame);
            await ETTask.CompletedTask;
        }
    }
}