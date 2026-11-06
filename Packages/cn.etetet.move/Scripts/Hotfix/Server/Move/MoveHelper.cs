using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端单位移动、停止与转向辅助扩展类。
    /// </summary>
    public static partial class MoveHelper
    {
        /// <summary>
        /// 服务端寻路并异步移动到目标坐标（可多次调用，新调用会自动打断上一次移动协程，并向 AOI 视野内广播路径与最终停止点）。
        /// </summary>
        // 可以多次调用，多次调用的话会取消上一次的协程
        public static async ETTask FindPathMoveToAsync(this Unit unit, float3 target, int turnTime = 100)
        {
            float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);
            if (speed < 0.01)
            {
                unit.SendStop(2);
                return;
            }

            M2C_PathfindingResult m2CPathfindingResult = M2C_PathfindingResult.Create();
            unit.GetComponent<PathfindingComponent>().Find(unit.Position, target, m2CPathfindingResult.Points);

            if (m2CPathfindingResult.Points.Count < 2)
            {
                unit.SendStop(3);
                return;
            }
                
            // 广播寻路路径
            m2CPathfindingResult.Id = unit.Id;
            MapMessageHelper.NoticeClient(unit, m2CPathfindingResult, NoticeType.Broadcast);

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            
            EntityRef<Unit> unitRef = unit;
            
            bool ret = await moveComponent.MoveToAsync(m2CPathfindingResult.Points, speed, turnTime);
            if (ret) // 如果返回false，说明被其它移动取消了，这时候不需要通知客户端stop
            {
                unit = unitRef;
                unit.SendStop(0);
            }
        }

        /// <summary>
        /// 停止单位当前的移动并向客户端广播停止消息。
        /// </summary>
        public static void Stop(this Unit unit, int error)
        {
            unit.GetComponent<MoveComponent>().Stop(error == 0);
            unit.SendStop(error);
        }

        /// <summary>
        /// 向客户端广播单位停止移动消息（<paramref name="error"/> 为 0 表示走完全程正常停止）。
        /// </summary>
        // error: 0表示协程走完正常停止
        public static void SendStop(this Unit unit, int error)
        {
            M2C_Stop m2CStop = M2C_Stop.Create();
            m2CStop.Error = error;
            m2CStop.Id = unit.Id;
            m2CStop.Position = unit.Position;
            m2CStop.Rotation = unit.Rotation;
            
            MapMessageHelper.NoticeClient(unit, m2CStop, NoticeType.Broadcast);
        }

        /// <summary>
        /// 控制单位在指定时间内平滑转向目标四元数，并向 AOI 广播转向消息。
        /// </summary>
        public static void Turn(this Unit unit, quaternion to, int turnTime)
        {
            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            turnComponent.Turn(to, turnTime);
            M2C_Turn m2CTurn = M2C_Turn.Create();
            m2CTurn.UnitId = unit.Id;
            m2CTurn.Rotation = to;
            m2CTurn.TurnTime = turnTime;
            MapMessageHelper.NoticeClient(unit, m2CTurn, NoticeType.Broadcast);
        }
    }
}