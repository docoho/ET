namespace ET
{
    /// <summary>
    /// 状态同步全局常量配置扩展
    /// </summary>
    public static partial class ConstValue
    {
        /// <summary>
        /// 路由服务（Router）HTTP 列表查询默认主机地址
        /// </summary>
        public const string RouterHttpHost = "127.0.0.1";

        /// <summary>
        /// 路由服务（Router）HTTP 列表查询默认端口
        /// </summary>
        public const int RouterHttpPort = 30300;

        /// <summary>
        /// 网络会话（Session）超时断开时间（毫秒，默认 30 秒）
        /// </summary>
        public const int SessionTimeoutTime = 30 * 1000;
        
    }
}