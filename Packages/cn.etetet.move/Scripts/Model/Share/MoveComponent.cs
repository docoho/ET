using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位路径移动组件（存储路径点列表、当前路段插值时间、移动速度与平滑转向四元数等状态）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class MoveComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前路段的起点坐标（即 <c>Targets[N - 1]</c>）。
        /// </summary>
        public float3 PreTarget
        {
            get
            {
                return this.Targets[this.N - 1];
            }
        }

        /// <summary>
        /// 当前路段的目标点坐标（即 <c>Targets[N]</c>）。
        /// </summary>
        public float3 NextTarget
        {
            get
            {
                return this.Targets[this.N];
            }
        }

        /// <summary>
        /// 开启整段移动协程时的起始时间戳（毫秒）。
        /// </summary>
        // 开启移动协程的时间
        public long BeginTime;

        /// <summary>
        /// 当前路段开始移动的时间戳（毫秒）。
        /// </summary>
        // 每个点的开始时间
        public long StartTime { get; set; }

        /// <summary>
        /// 当前路段开始插值时的起始坐标。
        /// </summary>
        // 开启移动协程的Unit的位置
        public float3 StartPos;

        /// <summary>
        /// 整条移动路径的初始起点坐标（即 <c>Targets[0]</c>）。
        /// </summary>
        public float3 RealPos
        {
            get
            {
                return this.Targets[0];
            }
        }

        private long needTime;

        /// <summary>
        /// 走完当前路段所需的耗时（毫秒）。
        /// </summary>
        public long NeedTime
        {
            get
            {
                return this.needTime;
            }
            set
            {
                this.needTime = value;
            }
        }

        /// <summary>
        /// 每帧驱动位置插值的定时器 ID。
        /// </summary>
        public long MoveTimer;

        /// <summary>
        /// 当前移动速度（米/秒）。
        /// </summary>
        public float Speed; // m/s

        /// <summary>
        /// 用于通知 <c>MoveToAsync</c> 异步等待完成的任务源。
        /// </summary>
        public ETTask<bool> tcs;

        /// <summary>
        /// 寻路生成的完整路标点列表。
        /// </summary>
        public List<float3> Targets = new List<float3>();

        /// <summary>
        /// 整条路径的最终终点坐标。
        /// </summary>
        public float3 FinalTarget
        {
            get
            {
                return this.Targets[this.Targets.Count - 1];
            }
        }

        /// <summary>
        /// 当前正在朝向的目标路标点在 <see cref="Targets"/> 中的索引。
        /// </summary>
        public int N;

        /// <summary>
        /// 转向插值所需耗时（毫秒）。
        /// </summary>
        public int TurnTime;

        /// <summary>
        /// 是否仅在水平面（忽略 Y 轴高度差）计算转向。
        /// </summary>
        public bool IsTurnHorizontal;

        /// <summary>
        /// 当前路段开始转向时的起始旋转四元数。
        /// </summary>
        public quaternion From;

        /// <summary>
        /// 当前路段的目标旋转四元数。
        /// </summary>
        public quaternion To;
    }
}