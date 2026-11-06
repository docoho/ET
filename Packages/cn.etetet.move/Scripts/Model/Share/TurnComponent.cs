using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位独立转向组件（用于单位原地转向时的四元数平滑插值）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class TurnComponent: Entity, IAwake
    {
        /// <summary>
        /// 开始转向的时间戳（毫秒）。
        /// </summary>
        public long StartTime;
        
        /// <summary>
        /// 完成转向所需的耗时（毫秒）。
        /// </summary>
        public int TurnTime;

        /// <summary>
        /// 是否仅水平转向。
        /// </summary>
        public bool IsTurnHorizontal;

        /// <summary>
        /// 起始旋转四元数。
        /// </summary>
        public quaternion From;

        /// <summary>
        /// 目标旋转四元数。
        /// </summary>
        public quaternion To;

        /// <summary>
        /// 每帧转向驱动定时器 ID。
        /// </summary>
        public long timer;
    }
}