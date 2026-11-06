using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现帮助类，提供公共的过滤、索引和本地缓存维护逻辑
    /// </summary>
    public static class ServiceDiscoveryHelper
    {
        /// <summary>
        /// 深拷贝一份元数据键值字典（<see cref="StringKV"/>）。
        /// </summary>
        public static StringKV CloneMetadata(StringKV metadata)
        {
            return metadata == null ? new StringKV() : new StringKV(metadata);
        }

        /// <summary>
        /// 将源元数据字典中的所有键值对复制到目标字典中（先清空目标字典）。
        /// </summary>
        public static void CopyMetadata(StringKV source, StringKV target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            if (source == null)
            {
                return;
            }

            foreach ((string key, string value) in source)
            {
                target[key] = value;
            }
        }

        /// <summary>
        /// 创建并填充一个网络传输用的 <see cref="ServiceInfoProto"/> 对象。
        /// </summary>
        public static ServiceInfoProto CreateServiceInfoProto(string sceneName, ActorId actorId, StringKV metadata)
        {
            ServiceInfoProto proto = ServiceInfoProto.Create();
            proto.SceneName = sceneName;
            proto.ActorId = actorId;
            CopyMetadata(metadata, proto.Metadata);
            return proto;
        }

        /// <summary>
        /// 判断已有 <see cref="ServiceInfo"/> 实体与给定的 ActorId 及元数据是否完全一致。
        /// </summary>
        public static bool IsSameService(ServiceInfo serviceInfo, ActorId actorId, StringKV metadata)
        {
            return serviceInfo != null && IsSameService(serviceInfo.ActorId, serviceInfo.Metadata, actorId, metadata);
        }

        /// <summary>
        /// 判断两组服务的 ActorId 与元数据键值对集合是否完全相等。
        /// </summary>
        public static bool IsSameService(ActorId oldActorId, StringKV oldMetadata, ActorId newActorId, StringKV newMetadata)
        {
            if (oldActorId != newActorId)
            {
                return false;
            }

            int oldCount = oldMetadata?.Count ?? 0;
            int newCount = newMetadata?.Count ?? 0;
            if (oldCount != newCount)
            {
                return false;
            }

            if (oldCount == 0)
            {
                return true;
            }

            if (newMetadata == null)
            {
                return false;
            }

            foreach ((string key, string value) in oldMetadata)
            {
                if (!newMetadata.TryGetValue(key, out string newValue) || newValue != value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 校验必填字符串参数是否非空白。
        /// </summary>
        public static bool TryValidateRequiredText(string value, string requestName, string fieldName, out string errorMessage)
        {
            errorMessage = null;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            errorMessage = $"{requestName} invalid: {fieldName} is empty.";
            return false;
        }

        /// <summary>
        /// 校验必填 <see cref="ActorId"/> 参数是否非默认值。
        /// </summary>
        public static bool TryValidateRequiredActorId(ActorId actorId, string requestName, string fieldName, out string errorMessage)
        {
            errorMessage = null;
            if (actorId != default)
            {
                return true;
            }

            errorMessage = $"{requestName} invalid: {fieldName} is empty.";
            return false;
        }

        /// <summary>
        /// 校验元数据字典中是否不包含空 Key 或 null Value。
        /// </summary>
        public static bool TryValidateMetadataMap(StringKV metadata, string requestName, string fieldName, out string errorMessage)
        {
            errorMessage = null;
            if (metadata == null)
            {
                return true;
            }

            foreach ((string key, string value) in metadata)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    errorMessage = $"{requestName} invalid: {fieldName} contains empty key.";
                    return false;
                }

                if (value == null)
                {
                    errorMessage = $"{requestName} invalid: {fieldName}[{key}] is null.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 校验 Agent 全量注册上报的本进程服务列表是否合法（SceneName 与 ActorId 非空，且服务进程地址与 Agent 进程地址一致）。
        /// </summary>
        public static bool TryValidateAgentLocalServices(List<ServiceInfoProto> localServices, ActorId agentActorId,
            string requestName, out string errorMessage)
        {
            errorMessage = null;
            if (localServices == null)
            {
                return true;
            }

            foreach (ServiceInfoProto serviceInfoProto in localServices)
            {
                if (serviceInfoProto == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(serviceInfoProto.SceneName) || serviceInfoProto.ActorId == default)
                {
                    errorMessage = $"{requestName} invalid: LocalServices contains empty service.";
                    return false;
                }

                if (serviceInfoProto.ActorId.Address != agentActorId.Address)
                {
                    errorMessage = $"{requestName} invalid: LocalServices address mismatch.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断目标服务的元数据是否满足过滤条件（支持逗号分隔的多值或匹配，所有 Key 之间为与关系）。
        /// </summary>
        public static bool MatchesMetadataFilter(StringKV metadata, StringKV filterMetadata)
        {
            if (filterMetadata == null || filterMetadata.Count == 0)
            {
                return true;
            }

            if (metadata == null)
            {
                return false;
            }

            foreach ((string key, string expectValue) in filterMetadata)
            {
                if (string.IsNullOrEmpty(key) || expectValue == null)
                {
                    return false;
                }

                if (!metadata.TryGetValue(key, out string actualValue))
                {
                    return false;
                }

                if (!IsMetadataValueMatched(actualValue, expectValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 重置重试计数、熔断截止时间与下次重试时间。
        /// </summary>
        public static void ResetRetryState(ref int failureCount, ref long circuitOpenUntil, ref long nextRetryTime)
        {
            failureCount = 0;
            circuitOpenUntil = 0;
            nextRetryTime = 0;
        }

        /// <summary>
        /// 记录一次失败重试，计算下次指数退避时间，并在连续失败达到阈值时开启熔断器。
        /// </summary>
        public static bool RecordRetryFailure(ref int failureCount, ref long circuitOpenUntil, ref long nextRetryTime,
            int circuitThreshold, long circuitOpenDuration, long baseBackoff, long maxBackoff, long now)
        {
            ++failureCount;
            long backoff = GetRetryBackoff(failureCount, baseBackoff, maxBackoff);
            nextRetryTime = now + backoff;
            if (failureCount >= circuitThreshold)
            {
                circuitOpenUntil = now + circuitOpenDuration;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据失败次数计算指数退避时长（不超过 <paramref name="maxBackoff"/>）。
        /// </summary>
        public static long GetRetryBackoff(int failureCount, long baseBackoff, long maxBackoff)
        {
            baseBackoff = baseBackoff > 0 ? baseBackoff : 100;
            maxBackoff = maxBackoff > 0 ? maxBackoff : 2 * 1000;
            long backoff = baseBackoff;
            int exponent = failureCount - 1;
            while (exponent > 0 && backoff < maxBackoff)
            {
                if (backoff > maxBackoff / 2)
                {
                    backoff = maxBackoff;
                    break;
                }

                backoff *= 2;
                --exponent;
            }

            return backoff > maxBackoff ? maxBackoff : backoff;
        }

        /// <summary>
        /// 获取单调递增的服务端时间戳（防止系统时钟回拨导致租约、心跳或重试计时器倒退）。
        /// </summary>
        public static long GetMonotonicNow(Scene scene, string traceName, ref long lastRawNow, ref long monotonicNow)
        {
            long rawNow = scene.GetSingleton<TimeInfo>().ServerNow();
            if (rawNow < lastRawNow)
            {
                long rollback = lastRawNow - rawNow;
                if (rollback >= 1000)
                {
                    Log.Warning(
                        $"{traceName} clock rollback detected scene: {scene?.Name} rollback: {rollback}ms rawNow: {rawNow} lastRaw: {lastRawNow}");
                }
            }

            lastRawNow = rawNow;
            if (rawNow >= monotonicNow)
            {
                monotonicNow = rawNow;
            }

            return monotonicNow;
        }

        /// <summary>
        /// 将服务的指定索引字段（如 SceneType、Zone）加入二级倒排索引表。
        /// </summary>
        public static void AddToIndexes(Dictionary<string, MultiMapSet<string, string>> servicesIndexs, string[] indexs, string sceneName,
            StringKV metadata)
        {
            if (servicesIndexs == null || indexs == null || string.IsNullOrEmpty(sceneName) || metadata == null)
            {
                return;
            }

            foreach ((string key, string value) in metadata)
            {
                if (!ShouldIndex(indexs, key))
                {
                    continue;
                }

                if (!servicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                {
                    index = new MultiMapSet<string, string>();
                    servicesIndexs.Add(key, index);
                }

                index.Add(value, sceneName);
            }
        }

        /// <summary>
        /// 将服务从二级倒排索引表中移除。
        /// </summary>
        public static void RemoveFromIndexes(Dictionary<string, MultiMapSet<string, string>> servicesIndexs, string[] indexs,
            string sceneName, StringKV metadata)
        {
            if (servicesIndexs == null || indexs == null || string.IsNullOrEmpty(sceneName) || metadata == null)
            {
                return;
            }

            foreach ((string key, string value) in metadata)
            {
                if (!ShouldIndex(indexs, key))
                {
                    continue;
                }

                if (servicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                {
                    index.Remove(value, sceneName);
                }
            }
        }

        /// <summary>
        /// 从本地服务字典与二级索引表中移除并销毁指定名称的 <see cref="ServiceInfo"/> 实体。
        /// </summary>
        public static bool RemoveLocalServiceCache(Dictionary<string, EntityRef<ServiceInfo>> sceneNameServices,
            Dictionary<string, MultiMapSet<string, string>> servicesIndexs, string[] indexs, string sceneName)
        {
            if (sceneNameServices == null || servicesIndexs == null || string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            if (!sceneNameServices.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceInfoRef))
            {
                return false;
            }

            using ServiceInfo serviceInfo = serviceInfoRef;
            sceneNameServices.Remove(sceneName);

            if (serviceInfo == null)
            {
                return true;
            }

            RemoveFromIndexes(servicesIndexs, indexs, sceneName, serviceInfo.Metadata);
            return true;
        }

        /// <summary>
        /// 通用的服务查询方法，支持索引优化和多值查询
        /// </summary>
        /// <param name="services">服务字典</param>
        /// <param name="servicesIndexs">服务索引字典</param>
        /// <param name="filterMetadata">过滤条件</param>
        /// <returns>匹配的服务列表</returns>
        public static List<ServiceInfo> GetServiceInfoByFilter(
            Dictionary<string, EntityRef<ServiceInfo>> services,
            Dictionary<string, MultiMapSet<string, string>> servicesIndexs,
            StringKV filterMetadata)
        {
            List<ServiceInfo> serviceInfos = new();
            if (services == null)
            {
                return serviceInfos;
            }

            if (filterMetadata == null || filterMetadata.Count == 0)
            {
                // 没有过滤条件，返回所有服务
                foreach ((_, EntityRef<ServiceInfo> serviceRef) in services)
                {
                    ServiceInfo sInfo = serviceRef;
                    if (sInfo != null)
                    {
                        serviceInfos.Add(sInfo);
                    }
                }
                return serviceInfos;
            }

            // 优先使用索引查询
            HashSet<string> candidateSceneNames = null;
            bool firstIndexedFilter = true;

            // 遍历过滤条件，优先使用有索引的字段
            foreach ((string key, string value) in filterMetadata)
            {
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    return serviceInfos;
                }

                // 检查是否有索引
                if (servicesIndexs != null && servicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                {
                    HashSet<string> matchedSceneNames = new HashSet<string>();

                    // 只有包含逗号时才进行分割，优化性能
                    if (value.Contains(','))
                    {
                        // 支持多值查询，用逗号分割
                        string[] values = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string singleValue in values)
                        {
                            string trimmedValue = singleValue.Trim();
                            HashSet<string> singleMatchedNames = index[trimmedValue];
                            matchedSceneNames.UnionWith(singleMatchedNames);
                        }
                    }
                    else
                    {
                        // 单值查询，直接获取索引结果
                        HashSet<string> singleMatchedNames = index[value];
                        matchedSceneNames.UnionWith(singleMatchedNames);
                    }

                    if (matchedSceneNames.Count == 0)
                    {
                        // 如果某个索引字段没有匹配结果，直接返回空列表
                        return serviceInfos;
                    }

                    if (firstIndexedFilter)
                    {
                        // 第一个索引过滤条件，直接使用结果集
                        candidateSceneNames = new HashSet<string>(matchedSceneNames);
                        firstIndexedFilter = false;
                    }
                    else
                    {
                        // 后续索引过滤条件，取交集
                        candidateSceneNames.IntersectWith(matchedSceneNames);
                        if (candidateSceneNames.Count == 0)
                        {
                            // 交集为空，直接返回
                            return serviceInfos;
                        }
                    }
                }
            }

            // 如果使用了索引查询
            if (candidateSceneNames != null)
            {
                // 从候选集合中筛选最终结果
                foreach (string sceneName in candidateSceneNames)
                {
                    if (services.TryGetValue(sceneName, out var serviceInfoRef))
                    {
                        ServiceInfo sInfo = serviceInfoRef;
                        if (sInfo != null)
                        {
                            if (MatchesMetadataFilter(sInfo.Metadata, filterMetadata))
                            {
                                serviceInfos.Add(sInfo);
                            }
                        }
                    }
                }
            }
            else
            {
                // 如果没有任何索引字段，回退到原来的全遍历方式
                foreach ((_, EntityRef<ServiceInfo> serviceRef) in services)
                {
                    ServiceInfo sInfo = serviceRef;
                    if (sInfo != null && MatchesMetadataFilter(sInfo.Metadata, filterMetadata))
                    {
                        serviceInfos.Add(sInfo);
                    }
                }
            }

            return serviceInfos;
        }

        /// <summary>
        /// 判断单个元数据字段的实际值是否匹配期望值（支持逗号分隔的多值或匹配）。
        /// </summary>
        private static bool IsMetadataValueMatched(string actualValue, string expectValue)
        {
            if (!expectValue.Contains(','))
            {
                return actualValue == expectValue;
            }

            string[] expectValues = expectValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in expectValues)
            {
                if (actualValue == value.Trim())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定的元数据 Key 是否配置在二级索引字段数组中。
        /// </summary>
        private static bool ShouldIndex(string[] indexs, string key)
        {
            return !string.IsNullOrEmpty(key) && Array.IndexOf(indexs, key) >= 0;
        }
    }
}
