using System;

namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（<see cref="SceneType.ServiceDiscovery"/>）处理增量单服务注销请求（<see cref="ServiceUnregisterRequest"/>）的处理器。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnregisterRequestHandler : MessageHandler<Scene, ServiceUnregisterRequest, ServiceUnregisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnregisterRequest request, ServiceUnregisterResponse response)
        {
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceUnregisterRequest),
                    nameof(request.SceneName), out string errorMessage))
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

                await serviceDiscovery.UnregisterServiceAsync(request.SceneName);
            }
            catch (Exception e)
            {
                serviceDiscovery = serviceDiscoveryRef;
                if (serviceDiscovery == null)
                {
                    return;
                }
                ServiceDiscoveryErrorHelper.SetPersistenceFailed(response, e.Message);
                Log.Error($"Service unregister failed scene: {request.SceneName} error: {e}");
            }
        }
    }
}
