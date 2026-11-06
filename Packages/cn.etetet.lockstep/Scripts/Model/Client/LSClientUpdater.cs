using TrueSync;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端预测与帧上报更新器组件（挂载在客户端 <see cref="Room"/> 上）
    /// </summary>
    [ComponentOf(typeof(Room))]
    public class LSClientUpdater: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 本地玩家当前帧采集到的操作输入（由表现层 <c>LSOperaComponent</c> 实时写入）
        /// </summary>
        public LSInput Input = new();
        
        /// <summary>
        /// 本地玩家的 PlayerId
        /// </summary>
        public long MyId { get; set; }
    }
}