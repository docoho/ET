using System;

namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（<see cref="SceneType.ServiceDiscovery"/>）处理增量单服务注册请求（<see cref="ServiceRegisterRequest"/>）的处理器。
    /// 校验当前节点为活跃 Master（带 Fencing 续租校验）后，注册该服务并向全网 Agent 广播变更通知。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceRegisterRequestHandler : MessageHandler<Scene, ServiceRegisterRequest, ServiceRegisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceRegisterRequest request, ServiceRegisterResponse response)
        {
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceRegisterRequest), nameof(request.SceneName),
                    out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateRequiredActorId(request.ActorId, nameof(ServiceRegisterRequest), nameof(request.ActorId),
                    out errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(request.Metadata, nameof(ServiceRegisterRequest), nameof(request.Metadata),
                    out errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            EntityRef<ServiceDiscovery> serviceDiscoveryRef = serviceDiscovery;
            try
            {
                bool isMaster = await serviceDiscovery.EnsureActiveMasterWithFenceAsync();
                serviceDiscovery = serviceDiscoveryRef;
                if (serviceDiscovery == null)
                {
                    return;
                }
                if (!isMaster)
                {
                    ServiceDiscoveryErrorHelper.SetNotWritableMaster(response, serviceDiscovery);
                    return;
                }

                await serviceDiscovery.RegisterServiceAsync(request.SceneName, request.ActorId, request.Metadata);
            }
            catch (Exception e)
            {
                serviceDiscovery = serviceDiscoveryRef;
                if (serviceDiscovery == null)
                {
                    return;
                }
                ServiceDiscoveryErrorHelper.SetPersistenceFailed(response, e.Message);
                Log.Error($"Service register failed scene: {request.SceneName} error: {e}");
            }
        }
    }
}
