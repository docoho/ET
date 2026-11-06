using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Location 主节点稳定选举辅助类。
    /// 所有 Location 节点与 <see cref="LocationProxyComponent"/> 均使用相同的确定性排序规则（最小 <see cref="ServiceMetaKey.PriorityId"/> 优先，相同时按 <c>SceneName</c> 字典序）选出唯一的 Primary Location 节点。
    /// </summary>
    public static class LocationPrimaryHelper
    {
        /// <summary>
        /// 从服务发现的 <see cref="ServiceInfo.Metadata"/> 中解析出无符号的 <c>PriorityId</c>。
        /// </summary>
        public static ulong GetPriorityId(ServiceInfo serviceInfo)
        {
            if (serviceInfo?.Metadata == null)
            {
                throw new RpcException(ErrorCode.ERR_LocationPrimaryUnavailable,
                    $"location service metadata missing scene: {serviceInfo?.SceneName}");
            }

            if (!serviceInfo.Metadata.TryGetValue(ServiceMetaKey.PriorityId, out string rawPriorityId)
                || string.IsNullOrWhiteSpace(rawPriorityId))
            {
                throw new RpcException(ErrorCode.ERR_LocationPrimaryUnavailable,
                    $"location service priority missing scene: {serviceInfo.SceneName}");
            }

            if (ulong.TryParse(rawPriorityId, out ulong priorityId))
            {
                return priorityId;
            }

            if (!long.TryParse(rawPriorityId, out long signedPriorityId))
            {
                throw new RpcException(ErrorCode.ERR_LocationPrimaryUnavailable,
                    $"location service priority invalid scene: {serviceInfo.SceneName} priority: {rawPriorityId}");
            }

            return unchecked((ulong)signedPriorityId);
        }

        /// <summary>
        /// 从在线 Location 服务列表中按确定性最小 PriorityId 规则选出主节点（Primary）的 <see cref="ServiceInfo"/>。
        /// </summary>
        public static ServiceInfo SelectStablePrimaryServiceInfo(List<ServiceInfo> serviceInfos)
        {
            ulong selectedPriorityId = ulong.MaxValue;
            ServiceInfo selectedServiceInfo = null;

            foreach (ServiceInfo serviceInfo in serviceInfos)
            {
                ulong priorityId = GetPriorityId(serviceInfo);

                if (selectedServiceInfo == null
                    || priorityId < selectedPriorityId
                    || (priorityId == selectedPriorityId && string.CompareOrdinal(serviceInfo.SceneName, selectedServiceInfo.SceneName) < 0))
                {
                    selectedPriorityId = priorityId;
                    selectedServiceInfo = serviceInfo;
                }
            }

            if (selectedServiceInfo == null)
            {
                throw new RpcException(ErrorCode.ERR_LocationPrimaryUnavailable, "not found location scene");
            }

            return selectedServiceInfo;
        }

        /// <summary>
        /// 从在线 Location 服务列表中选出主节点的场景名称。
        /// </summary>
        public static string SelectStablePrimarySceneName(List<ServiceInfo> serviceInfos)
        {
            return SelectStablePrimaryServiceInfo(serviceInfos).SceneName;
        }
    }
}
