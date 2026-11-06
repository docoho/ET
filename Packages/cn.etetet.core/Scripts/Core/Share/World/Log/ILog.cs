using System;

namespace ET
{
    /// <summary>
    /// 日志底层输出抽象接口（在客户端由 <c>UnityLogger</c> 实现，在服务端由 <c>NLogAdapter</c> 实现）。
    /// </summary>
    public interface ILog
    {
        /// <summary>设置当前日志归属的场景/纤程名称（用于日志文件分流或前缀标记）</summary>
        string SceneName { set; }


        void Trace(string message);
        void Warning(string message);
        void Info(string message);
        void Debug(string message);
        void Error(string message);
        void Error(Exception e);
        
#if DOTNET
        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message);
        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message);
        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message);
        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message);
        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message);
#endif
    }
}