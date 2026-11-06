using System;

namespace ET.Server
{
    /// <summary>
    /// 服务端进程网络地址单例（存储在全局 <see cref="World"/> 中）。
    /// 记录当前服务端进程的内网监听地址（<see cref="InnerAddress"/>）与外网暴露地址（<see cref="OuterAddress"/>）。
    /// 支持在云原生/Aspire 容器环境下直接从环境变量（<c>InnerIP</c>、<c>InnerPort</c>、<c>OuterIP</c>、<c>OuterPort</c>）初始化。
    /// </summary>
    public class AddressSingleton: Singleton<AddressSingleton>, ISingletonAwake
    {
        /// <summary>当前进程的内网通信 IP。</summary>
        public string InnerIP { get; set; }
        /// <summary>当前进程的内网监听端口。</summary>
        public int InnerPort { get; set; }
        /// <summary>当前进程对外暴露的公网/外网 IP。</summary>
        public string OuterIP { get; set; }
        /// <summary>当前进程对外暴露的外网端口（若单进程仅单 Scene 且由 Aspire 注入时大于 0）。</summary>
        public int OuterPort { get; set; }
        
        /// <summary>
        /// 单例唤醒时尝试从系统环境变量读取预配置的内外网 IP 与端口。
        /// </summary>
        public void Awake()
        {
            this.InnerIP = Environment.GetEnvironmentVariable("InnerIP");
            this.InnerPort = int.Parse(Environment.GetEnvironmentVariable("InnerPort") ?? "0");
            
            this.OuterIP = Environment.GetEnvironmentVariable("OuterIP");
            this.OuterPort = int.Parse(Environment.GetEnvironmentVariable("OuterPort") ?? "0");
        }
        
        /// <summary>
        /// 获取或设置当前进程的内网 <see cref="Address"/> 结构（由 <see cref="InnerIP"/> 和 <see cref="InnerPort"/> 组成）。
        /// </summary>
        public Address InnerAddress
        {
            get
            {
                return new Address(this.InnerIP, this.InnerPort);
            }
            set
            {
                this.InnerIP = value.IP;
                this.InnerPort = value.Port;
            }
        }
        
        /// <summary>
        /// 获取当前进程的外网 <see cref="Address"/> 结构（由 <see cref="OuterIP"/> 和 <see cref="OuterPort"/> 组成）。
        /// </summary>
        public Address OuterAddress
        {
            get
            {
                return new Address(this.OuterIP, this.OuterPort);
            }
        }
    }
}
