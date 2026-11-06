using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 网关登录凭据（SessionKey）缓存组件：存储 Realm 生成的短期一次性登录 Key 到账号名的映射。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class GateSessionKeyComponent : Entity, IAwake
    {
        /// <summary>登录 Key -> 账号名称（Account）字典</summary>
        public readonly Dictionary<long, string> sessionKey = new();
    }
}