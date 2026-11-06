using System;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;
using TrueSync;

namespace ET
{
    /// <summary>
    /// 帧同步逻辑战斗单位实体（继承自 <see cref="LSEntity"/>，全程使用 TrueSync 定点数存储位置与旋转以保证多端绝对确定性）
    /// </summary>
    [ChildOf(typeof(LSUnitComponent))]
    [MemoryPackable]
    public partial class LSUnit: LSEntity, IAwake, ISerializeToEntity
    {
        /// <summary>
        /// 定点数三维世界坐标
        /// </summary>
        public TSVector Position
        {
            get;
            set;
        }

        /// <summary>
        /// 定点数朝向向量（由 <see cref="Rotation"/> 换算得出，不直接参与快照序列化）
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public TSVector Forward
        {
            get => this.Rotation * TSVector.forward;
            set => this.Rotation = TSQuaternion.LookRotation(value, TSVector.up);
        }
        
        /// <summary>
        /// 定点数旋转四元数
        /// </summary>
        public TSQuaternion Rotation
        {
            get;
            set;
        }
    }
}