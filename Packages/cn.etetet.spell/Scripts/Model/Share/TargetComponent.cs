using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位当前锁定的目标组件，挂载在施法者 Unit 上，记录玩家或 AI 当前选中的目标单位及目标地面坐标。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class TargetComponent: Entity, IAwake
    {
        private EntityRef<Unit> unit;

        /// <summary>
        /// 当前锁定的目标 Unit 实体。
        /// </summary>
        public Unit Unit
        {
            get
            {
                return this.unit; 
                
            }
            set 
            {
                this.unit = value;
            }
        }
        /// <summary>
        /// 当前选定的目标地面坐标点。
        /// </summary>
        public float3 Position { get; set; }
    }
}