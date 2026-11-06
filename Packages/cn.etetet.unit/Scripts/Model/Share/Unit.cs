using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 游戏场景中的核心单位实体（可代表玩家角色、怪物、NPC、宠物或技能召唤物等）。
    /// <remarks>
    /// 修改 <see cref="Position"/> 或 <see cref="Rotation"/> 时会自动向当前所属 <see cref="Scene"/> 发布 <see cref="ChangePosition"/> 或 <see cref="ChangeRotation"/> 事件，
    /// 从而解耦地驱动 AOI 视野更新、表现层 Transform 同步或移动打断 Buff 等逻辑。
    /// </remarks>
    /// </summary>
    [ChildOf(typeof(UnitComponent))]
    [DebuggerDisplay("ViewName,nq")]
    public partial class Unit: Entity, IAwake<int>
    {
        /// <summary>
        /// 单位配置表 ID（对应 <see cref="UnitConfig"/>）。
        /// </summary>
        public int ConfigId { get; set; } //配置表id
        
        /// <summary>
        /// 单位类型枚举（如玩家、怪物、NPC、宠物等）。
        /// </summary>
        public UnitType UnitType { get; set; }

        /// <summary>
        /// 内部持久化的三维空间坐标字段。
        /// </summary>
        [BsonElement]
        private float3 position; //坐标

        /// <summary>
        /// 获取或设置单位的三维坐标（设置时会发布 <see cref="ChangePosition"/> 事件通知 AOI 与表现层）。
        /// </summary>
        [BsonIgnore]
        public float3 Position
        {
            get => this.position;
            set
            {
                float3 oldPos = this.position;
                this.position = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangePosition() { Unit = this, OldPos = oldPos });
            }
        }

        /// <summary>
        /// 获取或设置单位的正前方朝向向量（基于 <see cref="Rotation"/> 换算）。
        /// </summary>
        [BsonIgnore]
        public float3 Forward
        {
            get => math.mul(this.Rotation, math.forward());
            set => this.Rotation = quaternion.LookRotation(value, math.up());
        }
        
        /// <summary>
        /// 内部持久化的旋转四元数字段。
        /// </summary>
        [BsonElement]
        private quaternion rotation;
        
        /// <summary>
        /// 获取或设置单位的旋转四元数（设置时会发布 <see cref="ChangeRotation"/> 事件通知表现层）。
        /// </summary>
        [BsonIgnore]
        public quaternion Rotation
        {
            get => this.rotation;
            set
            {
                this.rotation = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangeRotation() { Unit = this });
            }
        }

        /// <summary>
        /// 调试器与层级视图中显示的实体名称。
        /// </summary>
        protected override string ViewName
        {
            get
            {
                return $"{this.GetType().FullName} ({this.Id})";
            }
        }
    }
}