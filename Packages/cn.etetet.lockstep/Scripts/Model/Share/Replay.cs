using System.Collections.Generic;
using MemoryPack;

namespace ET
{
    /// <summary>
    /// 帧同步对局录像数据容器，记录初始单位信息、每一帧的全员权威输入以及周期性 <see cref="LSWorld"/> 关键帧快照
    /// </summary>
    [MemoryPackable]
    public partial class Replay: Object
    {
        /// <summary>
        /// 对局开始时的所有参赛单位初始状态列表
        /// </summary>
        [MemoryPackOrder(1)]
        public List<LockStepUnitInfo> UnitInfos;
        
        /// <summary>
        /// 从第 0 帧开始的完整每帧玩家输入序列
        /// </summary>
        [MemoryPackOrder(2)]
        public List<OneFrameInputs> FrameInputs = new();
        
        /// <summary>
        /// 每隔 <see cref="LSConstValue.SaveLSWorldFrameCount"/> 帧保存一次的 <see cref="LSWorld"/> 二进制快照列表（用于录像拖拽跳转时快速定位恢复）
        /// </summary>
        [MemoryPackOrder(3)]
        public List<byte[]> Snapshots = new();
    }
}