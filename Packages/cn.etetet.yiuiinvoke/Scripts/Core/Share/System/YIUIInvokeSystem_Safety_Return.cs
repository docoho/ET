using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// YIUI跨层调用系统（安全带返回值调用SafetyInvokeReturn与安全异步调用SafetyInvokeTask分部类，未注册时不报错并返回默认值）。
    /// </summary>
    public partial class YIUIInvokeSystem
    {
        /// <summary>安全执行带返回值的YIUI调用，若未注册对应处理器则静默返回 defaultReturn。</summary>
        public R SafetyInvokeReturn<T, R>(T self, string invokeType, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, returnList);
        }

        public R SafetyInvokeReturn<T, T1, R>(T self, string invokeType, T1 arg1, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<T1, R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, arg1, returnList);
        }

        public R SafetyInvokeReturn<T, T1, T2, R>(T self, string invokeType, T1 arg1, T2 arg2, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<T1, T2, R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, arg1, arg2, returnList);
        }

        public R SafetyInvokeReturn<T, T1, T2, T3, R>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<T1, T2, T3, R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, arg1, arg2, arg3, returnList);
        }

        public R SafetyInvokeReturn<T, T1, T2, T3, T4, R>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<T1, T2, T3, T4, R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, arg1, arg2, arg3, arg4, returnList);
        }

        public R SafetyInvokeReturn<T, T1, T2, T3, T4, T5, R>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, R defaultReturn = default, List<R> returnList = null) where T : Entity
        {
            if (!CheckInvokeReturn<T1, T2, T3, T4, T5, R>(invokeType))
            {
                return defaultReturn;
            }

            return InvokeReturn(self, invokeType, arg1, arg2, arg3, arg4, arg5, returnList);
        }

        public async ETTask SafetyInvokeTask<T>(T self, string invokeType) where T : Entity
        {
            if (!CheckInvokeTask(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType);
        }

        public async ETTask SafetyInvokeTask<T, T1>(T self, string invokeType, T1 arg1) where T : Entity
        {
            if (!CheckInvokeTask<T1>(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType, arg1);
        }

        public async ETTask SafetyInvokeTask<T, T1, T2>(T self, string invokeType, T1 arg1, T2 arg2) where T : Entity
        {
            if (!CheckInvokeTask<T1, T2>(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType, arg1, arg2);
        }

        public async ETTask SafetyInvokeTask<T, T1, T2, T3>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3) where T : Entity
        {
            if (!CheckInvokeTask<T1, T2, T3>(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType, arg1, arg2, arg3);
        }

        public async ETTask SafetyInvokeTask<T, T1, T2, T3, T4>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : Entity
        {
            if (!CheckInvokeTask<T1, T2, T3, T4>(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType, arg1, arg2, arg3, arg4);
        }

        public async ETTask SafetyInvokeTask<T, T1, T2, T3, T4, T5>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : Entity
        {
            if (!CheckInvokeTask<T1, T2, T3, T4, T5>(invokeType))
            {
                return;
            }

            await InvokeTask(self, invokeType, arg1, arg2, arg3, arg4, arg5);
        }
    }
}