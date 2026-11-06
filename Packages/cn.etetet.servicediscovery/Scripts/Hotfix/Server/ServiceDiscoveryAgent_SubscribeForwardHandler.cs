using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理同进程业务场景（<see cref="ServiceDiscoveryProxy"/>）服务变更订阅请求的处理器。
    /// 确保 Agent 已向主节点注册就绪后，记录 Proxy 的订阅过滤条件，并在响应中立即返回当前匹配的初始服务快照。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_SubscribeForwardHandler :
        MessageHandler<Scene, ServiceSubscribeRequest, ServiceSubscribeResponse>
    {
        /// <summary>
        /// 处理同进程 Proxy 的服务订阅请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceSubscribeRequest request, ServiceSubscribeResponse response)
        {
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.SceneName, nameof(ServiceSubscribeRequest),
                    nameof(request.SceneName), out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (request.SubscriberActorId == default)
            {
                response.Error = ErrorCode.ERR_SubscriberActorIdRequired;
                response.Message = $"{nameof(ServiceSubscribeRequest)} invalid: SubscriberActorId is empty.";
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateRequiredText(request.FilterName, nameof(ServiceSubscribeRequest),
                    nameof(request.FilterName), out errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(request.FilterMetadata, nameof(ServiceSubscribeRequest),
                    nameof(request.FilterMetadata), out errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            string sceneName = scene.Name;
            try
            {
                // 确保 Agent 已与主节点完成握手并持有最新全量快照
                await agent.EnsureReadyAsync(nameof(ServiceSubscribeRequest));
                agent = agentRef;
                if (agent == null)
                {
                    return;
                }

                // 登记 Proxy 的订阅过滤条件，并将当前匹配该过滤条件的服务快照填入 response.Services
                int error = agent.SubscribeProxyServiceChange(request.SceneName, request.SubscriberActorId, request.FilterName,
                    request.FilterMetadata, response.Services);
                if (error != ErrorCode.ERR_Success)
                {
                    response.Error = error;
                    response.Message = nameof(ErrorCode.ERR_SubscriberActorIdRequired);
                }
            }
            catch (RpcException rpcException)
            {
                response.Error = rpcException.Error;
                response.Message = rpcException.Message;
            }
            catch (Exception e)
            {
                agent = agentRef;
                if (agent == null)
                {
                    return;
                }

                ServiceDiscoveryErrorHelper.SetInternalFailure(response, e.Message);
                Log.Warning($"ServiceDiscovery agent subscribe forward failed scene: {sceneName} error: {e.Message}");
            }
        }
    }
}
