using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 帧同步单个战斗单位的表现层视图实体（作为 <see cref="LSUnitViewComponent"/> 的子实体，实体 ID 与对应的逻辑层 <see cref="LSUnit"/> 相同）
    /// </summary>
    [ChildOf(typeof(LSUnitViewComponent))]
    public class LSUnitView: Entity, IAwake<GameObject>, IUpdate, ILSRollback
    {
        /// <summary>
        /// 单位关联的 Unity 场景 <see cref="UnityEngine.GameObject"/> 实例
        /// </summary>
        public GameObject GameObject { get; set; }

        /// <summary>
        /// 单位关联的 Unity <see cref="UnityEngine.Transform"/> 组件
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// 绑定的帧同步逻辑单位 <see cref="LSUnit"/> 弱引用
        /// </summary>
        public EntityRef<LSUnit> Unit;

        /// <summary>
        /// 当前插值目标世界坐标
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 当前插值目标旋转四元数
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// 本段位移插值预计总耗时（秒）
        /// </summary>
        public float totalTime;

        /// <summary>
        /// 本段位移插值已流逝时间（秒）
        /// </summary>
        public float t;
    }
}