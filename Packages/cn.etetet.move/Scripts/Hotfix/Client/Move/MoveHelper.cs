using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 客户端单位移动辅助扩展类。
    /// </summary>
    public static partial class MoveHelper
    {
        /// <summary>
        /// 向服务端发送寻路移动请求并异步等待单位停止移动（多次调用会自动取消上一次等待）。
        /// </summary>
        // 可以多次调用，多次调用的话会取消上一次的协程
        public static async ETTask<int> MoveToAsync(this Unit unit, float3 targetPos)
        {
            C2M_PathfindingResult msg = C2M_PathfindingResult.Create();
            msg.Position = targetPos;
            unit.Root().GetComponent<ClientSenderComponent>().Send(msg);

            ObjectWait objectWait = unit.GetComponent<ObjectWait>();
            
            // 要取消上一次的移动协程
            objectWait.Notify(new Wait_UnitStop() { Error = WaitTypeError.Cancel });
            
            // 一直等到unit发送stop
            Wait_UnitStop waitUnitStop = await objectWait.Wait<Wait_UnitStop>();
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            if (cancellationToken.IsCancel())
            {
                return WaitTypeError.Cancel;
            }
            return waitUnitStop.Error;
        }
        
        /// <summary>
        /// 按给定的路点列表与当前速度在本地直接驱动 <see cref="MoveComponent"/> 移动。
        /// </summary>
        public static async ETTask MoveToAsync(this Unit unit, List<float3> path)
        {
            float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);
            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            await moveComponent.MoveToAsync(path, speed);
        }
    }
}