using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位位置改变事件（当 <see cref="ET.Unit.Position"/> 更新时发布）。
    /// </summary>
    public struct ChangePosition
    {
        /// <summary>
        /// 位置发生变化的单位实体引用。
        /// </summary>
        public EntityRef<Unit> Unit;

        /// <summary>
        /// 改变前的旧坐标。
        /// </summary>
        public float3 OldPos;
    }

    /// <summary>
    /// 单位朝向旋转改变事件（当 <see cref="ET.Unit.Rotation"/> 更新时发布）。
    /// </summary>
    public struct ChangeRotation
    {
        /// <summary>
        /// 朝向发生变化的单位实体引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }
}