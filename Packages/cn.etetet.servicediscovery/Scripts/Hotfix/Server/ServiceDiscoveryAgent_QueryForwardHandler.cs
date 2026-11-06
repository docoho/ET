using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理（<see cref="ServiceDiscoveryAgent"/>）处理服务查询请求（<see cref="ServiceQueryRequest"/>）的处理器。
    /// 等待 Agent 初始化就绪后，直接基于 Agent 本地维护的全量服务缓存与索引按过滤条件返回匹配的服务列表。
    /// </summary>
    [MessageHandler(SceneType.ServiceDiscoveryAgent)]
    public class ServiceDiscoveryAgent_QueryForwardHandler :
        MessageHandler<Scene, ServiceQueryRequest, ServiceQueryResponse>
    {
        /// <summary>
        /// 处理服务按条件查询请求。
        /// </summary>
        protected override async ETTask Run(Scene scene, ServiceQueryRequest request, ServiceQueryResponse response)
        {
            // 校验过滤条件字典格式合法性
            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(request.Filter, nameof(ServiceQueryRequest), nameof(request.Filter),
                    out string errorMessage))
            {
                ServiceDiscoveryErrorHelper.SetInvalidArgument(response, errorMessage);
                return;
            }

            ServiceDiscoveryAgent agent = scene.GetComponent<ServiceDiscoveryAgent>();
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            string sceneName = scene.Name;
            try
            {
                // 确保 Agent 已向 Master 完成初始注册并同步全量快照后，查询本地缓存
                List<ServiceInfo> services = await agent.QueryLocalByFilterAsync(request.Filter);
                agent = agentRef;
                if (agent == null)
                {
                    return;
                }

                foreach (ServiceInfo serviceInfo in services)
                {
                    response.Services.Add(serviceInfo.ToProto());
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
                Log.Warning($"ServiceDiscovery agent query forward failed scene: {sceneName} error: {e.Message}");
            }
        }
    }
}
