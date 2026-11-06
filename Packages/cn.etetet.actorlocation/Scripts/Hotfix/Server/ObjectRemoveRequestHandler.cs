using System;

namespace ET.Server
{
    /// <summary>
    /// Location 场景处理实体位置移除请求（<see cref="ObjectRemoveRequest"/>）的处理器。
    /// </summary>
    [MessageHandler(SceneType.Location)]
    public class ObjectRemoveRequestHandler: MessageHandler<Scene, ObjectRemoveRequest, ObjectRemoveResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectRemoveRequest request, ObjectRemoveResponse response)
        {
            LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
            if (!locationComponent.EnsurePrimary(response))
            {
                return;
            }

            await locationComponent.Remove(request.Type, request.Key, request.ExpectedActorId);
        }
    }
}
