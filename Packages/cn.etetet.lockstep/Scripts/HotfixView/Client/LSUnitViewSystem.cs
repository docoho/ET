using System;
using TrueSync;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 帧同步单个单位视图实体系统，负责在渲染帧（Update）中将逻辑层 <see cref="LSUnit"/> 的定点数位置/朝向平滑插值同步到 Unity Transform 与动画状态机
    /// </summary>
    [EntitySystemOf(typeof(LSUnitView))]
    [LSEntitySystemOf(typeof(LSUnitView))]
    [FriendOf(typeof(LSUnitView))]
    public static partial class LSUnitViewSystem
    {
        /// <summary>
        /// 绑定单位对应的 Unity <see cref="GameObject"/> 与 <see cref="Transform"/>
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSUnitView self, GameObject go)
        {
            self.GameObject = go;
            self.Transform = go.transform;

        }

        /// <summary>
        /// 帧同步预测回滚发生时的表现层回调（当前保留插值平滑过渡，不硬切坐标）
        /// </summary>
        [LSEntitySystem]
        private static void LSRollback(this LSUnitView self)
        {
            //LSUnit unit = self.GetUnit();
            //self.Transform.position = unit.Position.ToVector();
            //self.Transform.rotation = unit.Rotation.ToQuaternion();
            //self.t = 0;
            //self.totalTime = 0;
        }

        /// <summary>
        /// 每个渲染帧读取逻辑单位 <see cref="LSUnit"/> 的最新定点数坐标与朝向，通过线性插值（Lerp）平滑驱动 Transform 移动与跑步动画切换
        /// </summary>
        [EntitySystem]
        private static void Update(this LSUnitView self)
        {
            LSUnit unit = self.GetUnit();

            Vector3 unitPos = unit.Position.ToVector();
            const float speed = 6f;
            float speed2 = speed;// * self.Room().SpeedMultiply;

            if (unitPos != self.Position)
            {
                float distance = (unitPos - self.Position).magnitude;
                self.totalTime = distance / speed2;
                self.t = 0;
                self.Position = unit.Position.ToVector();
                self.Rotation = unit.Rotation.ToQuaternion();
            }


            LSInput input = unit.GetComponent<LSInputComponent>().LSInput;
            if (input.V != TSVector2.zero)
            {
                self.GetComponent<LSAnimatorComponent>().SetFloatValue("Speed", speed2);
            }
            else
            {
                self.GetComponent<LSAnimatorComponent>().SetFloatValue("Speed", 0);
            }
            self.t += Time.deltaTime;
            self.Transform.rotation = Quaternion.Lerp(self.Transform.rotation, self.Rotation, self.t / 1f);
            self.Transform.position = Vector3.Lerp(self.Transform.position, self.Position, self.t / self.totalTime);
        }

        /// <summary>
        /// 获取当前视图关联的帧同步逻辑单位 <see cref="LSUnit"/>（由于回滚会重建 <see cref="LSWorld"/>，当 <see cref="EntityRef{T}"/> 失效时会自动按 ID 重新从新世界中查找绑定）
        /// </summary>
        private static LSUnit GetUnit(this LSUnitView self)
        {
            LSUnit unit = self.Unit;
            if (unit != null)
            {
                return unit;
            }

            self.Unit = (self.IScene as Room).LSWorld.GetComponent<LSUnitComponent>().GetChild<LSUnit>(self.Id);
            return self.Unit;
        }
    }
}