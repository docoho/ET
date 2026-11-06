using System;

namespace ET
{
    /// <summary>
    /// YIUI跨层调用系统（安全无返回值调用SafetyInvoke分部类，未注册对应处理器时静默忽略）。
    /// </summary>
    public partial class YIUIInvokeSystem
    {
        /// <summary>安全执行无返回值的YIUI调用，若未注册对应处理器则直接返回。</summary>
        public void SafetyInvoke<T>(T self, string invokeType) where T : Entity
        {
            if (!CheckInvoke(invokeType))
            {
                return;
            }

            Invoke(self, invokeType);
        }

        public void SafetyInvoke<T, T1>(T self, string invokeType, T1 arg1) where T : Entity
        {
            if (!CheckInvoke<T1>(invokeType))
            {
                return;
            }

            Invoke(self, invokeType, arg1);
        }

        public void SafetyInvoke<T, T1, T2>(T self, string invokeType, T1 arg1, T2 arg2) where T : Entity
        {
            if (!CheckInvoke<T1, T2>(invokeType))
            {
                return;
            }

            Invoke(self, invokeType, arg1, arg2);
        }

        public void SafetyInvoke<T, T1, T2, T3>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3) where T : Entity
        {
            if (!CheckInvoke<T1, T2, T3>(invokeType))
            {
                return;
            }

            Invoke(self, invokeType, arg1, arg2, arg3);
        }

        public void SafetyInvoke<T, T1, T2, T3, T4>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : Entity
        {
            if (!CheckInvoke<T1, T2, T3, T4>(invokeType))
            {
                return;
            }

            Invoke(self, invokeType, arg1, arg2, arg3, arg4);
        }

        public void SafetyInvoke<T, T1, T2, T3, T4, T5>(T self, string invokeType, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : Entity
        {
            if (!CheckInvoke<T1, T2, T3, T4, T5>(invokeType))
            {
                return;
            }

            Invoke(self, invokeType, arg1, arg2, arg3, arg4, arg5);
        }
    }
}