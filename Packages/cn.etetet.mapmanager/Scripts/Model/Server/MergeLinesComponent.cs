using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 待执行分线合并的任务信息结构体（将 <see cref="LineNum2"/> 合并至 <see cref="LineNum1"/>）。
    /// </summary>
    public struct MergeLineInfo
    {
        /// <summary>
        /// 保留的目标分线编号。
        /// </summary>
        public long LineNum1;

        /// <summary>
        /// 将被迁出并销毁的源分线编号。
        /// </summary>
        public long LineNum2;

        /// <summary>
        /// 发起合线判定的时间戳（毫秒）。
        /// </summary>
        public long Time;
    }
    
    /// <summary>
    /// 地图分线动态合线组件（挂载在分线类型的 <see cref="MapInfo"/> 上）。
    /// </summary>
    [ComponentOf(typeof(MapInfo))]
    public class MergeLinesComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 等待执行玩家迁移与分线合并的队列。
        /// </summary>
        public Queue<MergeLineInfo> WaitMergetQueue = new();
    }
}