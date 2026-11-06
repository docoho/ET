using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 帧同步房间根实体（双端共享，实现 <see cref="IScene"/>），统一管理权威帧号、客户端预测帧号、环形帧缓冲区 <see cref="FrameBuffer"/>、固定时间计数器 <see cref="FixedTimeCounter"/>、录像数据 <see cref="Replay"/> 以及子世界 <see cref="LSWorld"/>
    /// </summary>
    [ComponentOf]
    public class Room: Entity, IScene, IAwake, IUpdate
    {
        /// <summary>
        /// 房间所属纤程实例
        /// </summary>
        public Fiber Fiber { get; set; }

        /// <summary>
        /// 场景类型标识
        /// </summary>
        public int SceneType { get; set; }

        /// <summary>
        /// 房间或地图名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 对局正式开始的服务端时间戳（毫秒）
        /// </summary>
        public long StartTime { get; set; }

        // 帧缓存
        /// <summary>
        /// 环形帧缓冲区（缓存最近若干帧的输入、世界快照与哈希值）
        /// </summary>
        public FrameBuffer FrameBuffer { get; set; }

        // 计算fixedTime，fixedTime在客户端是动态调整的，会做时间膨胀缩放
        /// <summary>
        /// 固定时间步长计数器（客户端会根据服务端时间差动态微调单帧间隔）
        /// </summary>
        public FixedTimeCounter FixedTimeCounter { get; set; }

        // 玩家id列表
        /// <summary>
        /// 房间内所有参赛玩家的 ID 列表
        /// </summary>
        public List<long> PlayerIds { get; } = new(LSConstValue.MatchCount);
        
        // 预测帧
        /// <summary>
        /// 客户端当前已推进到的预测帧号
        /// </summary>
        public int PredictionFrame { get; set; } = -1;

        // 权威帧
        /// <summary>
        /// 当前已确认的服务端权威帧号
        /// </summary>
        public int AuthorityFrame { get; set; } = -1;

        // 存档
        /// <summary>
        /// 当前对局的录像存档数据对象
        /// </summary>
        public Replay Replay { get; set; } = new();

        private EntityRef<LSWorld> lsWorld;

        // LSWorld做成child，可以有多个lsWorld，比如守望先锋有两个
        /// <summary>
        /// 当前激活的帧同步逻辑世界实例（赋值时自动挂载为当前 <see cref="Room"/> 的子实体）
        /// </summary>
        public LSWorld LSWorld
        {
            get
            {
                return this.lsWorld;
            }
            set
            {
                this.AddChild(value);
                this.lsWorld = value;
            }
        }

        /// <summary>
        /// 当前房间是否处于录像回放模式
        /// </summary>
        public bool IsReplay { get; set; }
        
        /// <summary>
        /// 单次渲染帧内连续追赶执行的逻辑帧倍数
        /// </summary>
        public int SpeedMultiply { get; set; }
    }
}