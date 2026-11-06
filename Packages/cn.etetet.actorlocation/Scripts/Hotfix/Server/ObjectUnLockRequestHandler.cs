using System;

namespace ET.Server
{
    /// <summary>
    /// Location 场景处理实体位置解锁并更新新 ActorId 请求（<see cref="ObjectUnLockRequest"/>）的处理器。
    /// </summary>
    [MessageHandler(SceneType.Location)]
    public class ObjectUnLockRequestHandler: MessageHandler<Scene, ObjectUnLockRequest, ObjectUnLockResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectUnLockRequest request, ObjectUnLockResponse response)
        {
            LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
            if (!locationComponent.EnsurePrimary(response))
            {
                return;
            }

            await locationComponent.UnLock(request.Type, request.Key, request.OldActorId, request.NewActorId, request.LockToken);
        }
    }
}
