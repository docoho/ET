using System;
using TrueSync;

namespace ET
{
    /// <summary>
    /// 帧同步单位输入组件系统，在每个逻辑帧根据当前帧的玩家摇杆/按键输入使用 TrueSync 定点数更新单位坐标与朝向
    /// </summary>
    [EntitySystemOf(typeof(LSInputComponent))]
    [LSEntitySystemOf(typeof(LSInputComponent))]
    public static partial class LSInputComponentSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSInputComponent self)
        {

        }
        
        /// <summary>
        /// 确定性逻辑帧更新：根据定点数二维输入向量计算本帧位移（速度 6m/s，帧间隔 50ms）并更新单位位置与朝向
        /// </summary>
        /// <param name="self">帧同步输入组件实例</param>
        [LSEntitySystem]
        private static void LSUpdate(this LSInputComponent self)
        {
            LSUnit unit = self.GetParent<LSUnit>();

            TSVector2 v2 = self.LSInput.V * 6 * 50 / 1000;
            if (v2.LengthSquared() < 0.0001f)
            {
                return;
            }
            TSVector oldPos = unit.Position;
            unit.Position += new TSVector(v2.x, 0, v2.y);
            unit.Forward = unit.Position - oldPos;
        }
    }
}