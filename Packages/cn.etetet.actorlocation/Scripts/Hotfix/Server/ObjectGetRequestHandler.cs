using System;

namespace ET.Server
{
    /// <summary>
    /// Location 场景处理查询实体位置请求（<see cref="ObjectGetRequest"/>）的处理器。
    /// </summary>
    [MessageHandler(SceneType.Location)]
    public class ObjectGetRequestHandler: MessageHandler<Scene, ObjectGetRequest, ObjectGetResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectGetRequest request, ObjectGetResponse response)
        {
            LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
            if (!locationComponent.EnsurePrimary(response))
            {
                return;
            }

            response.ActorId = await locationComponent.Get(request.Type, request.Key);
        }
    }
}
