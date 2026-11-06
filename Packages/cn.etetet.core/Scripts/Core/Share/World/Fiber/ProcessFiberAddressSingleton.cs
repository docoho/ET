using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 进程内常驻功能纤程（按 <c>SceneType</c> 唯一标识，如 <c>SceneType.NetInner</c>）地址注册表（线程安全、支持子 Fiber 继承）。
    /// </summary>
    [CodeProcess]
    public class ProcessFiberAddressSingleton : Singleton<ProcessFiberAddressSingleton>, ISingletonAwake, IInheritableSingleton
    {
        private readonly ConcurrentDictionary<int, FiberInstanceId> fiberInstanceIds = new();

        public void Awake()
        {
        }

        /// <summary>
        /// 注册指定 <paramref name="sceneType"/> 对应的进程内纤程地址 <paramref name="fiberInstanceId"/>（同一进程内同一种 SceneType 只允许注册一个不同地址）。
        /// </summary>
        public void Register(int sceneType, FiberInstanceId fiberInstanceId)
        {
            if (sceneType == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sceneType), sceneType, "process fiber scene type cannot be 0");
            }

            if (fiberInstanceId == default)
            {
                throw new ArgumentException("process fiber instance id cannot be default", nameof(fiberInstanceId));
            }

            if (!this.fiberInstanceIds.TryGetValue(sceneType, out FiberInstanceId existing))
            {
                this.fiberInstanceIds.TryAdd(sceneType, fiberInstanceId);
                return;
            }

            if (existing == fiberInstanceId)
            {
                return;
            }

            throw new Exception(
                $"process fiber address already registered, sceneType: {sceneType}, existing: {existing}, new: {fiberInstanceId}");
        }

        /// <summary>
        /// 获取指定 <paramref name="sceneType"/> 的进程内纤程地址（若未注册则抛出异常）。
        /// </summary>
        public FiberInstanceId Get(int sceneType)
        {
            if (this.TryGet(sceneType, out FiberInstanceId fiberInstanceId))
            {
                return fiberInstanceId;
            }

            throw new Exception($"process fiber address not found, sceneType: {sceneType}");
        }

        /// <summary>
        /// 尝试获取指定 <paramref name="sceneType"/> 的进程内纤程地址。
        /// </summary>
        public bool TryGet(int sceneType, out FiberInstanceId fiberInstanceId)
        {
            return this.fiberInstanceIds.TryGetValue(sceneType, out fiberInstanceId);
        }

        /// <summary>
        /// 注销指定 <paramref name="sceneType"/> 的进程内纤程地址。
        /// </summary>
        public void Unregister(int sceneType, FiberInstanceId fiberInstanceId)
        {
            if (!this.fiberInstanceIds.TryGetValue(sceneType, out FiberInstanceId existing))
            {
                return;
            }

            if (existing != fiberInstanceId)
            {
                return;
            }

            this.fiberInstanceIds.TryRemove(sceneType, out _);
        }
    }
}
