using System;

namespace ET.Server
{
    /// <summary>
    /// Location 场景处理注册实体位置映射请求（<see cref="ObjectAddRequest"/>）的处理器。
    /// </summary>
    [MessageHandler(SceneType.Location)]
    public class ObjectAddRequestHandler: MessageHandler<Scene, ObjectAddRequest, ObjectAddResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectAddRequest request, ObjectAddResponse response)
        {
            LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
            // 仅允许当前 Primary Location 节点处理请求
            if (!locationComponent.EnsurePrimary(response))
            {
                return;
            }

            await locationComponent.Add(request.Type, request.Key, request.ActorId);
        }
    }
}
