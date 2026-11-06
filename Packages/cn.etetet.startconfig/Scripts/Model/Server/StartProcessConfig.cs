using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 服务端进程启动配置表管理类（由 Luban 生成其主体，此处标记 <c>[EnableClass]</c> 允许在 Model 层声明）。
    /// </summary>
    [EnableClass]
    public partial class StartProcessConfigCategory
    {
    }

    /// <summary>
    /// 服务端物理机/虚拟机网络配置表管理类（包含内网 IP、外网 IP 等信息）。
    /// </summary>
    [EnableClass]
    public partial class StartMachineConfigCategory
    {
    }

    /// <summary>
    /// 单个服务端进程启动配置数据项的分部扩展。
    /// </summary>
    public partial class StartProcessConfig
    {
        partial void EndInit()
        {
        }
    }

    /// <summary>
    /// <see cref="StartProcessConfig"/> 的网络地址与机器信息查询扩展方法。
    /// </summary>
    public static class StartProcessConfigExtensions
    {
        /// <summary>
        /// 获取当前进程所属物理机/宿主机的 <see cref="StartMachineConfig"/> 配置。
        /// </summary>
        public static StartMachineConfig GetStartMachineConfig(this StartProcessConfig self, Fiber fiber)
        {
            return fiber.GetSingleton<StartMachineConfigCategory>().Get(self.MachineId);
        }

        /// <summary>
        /// 获取当前进程所在机器的内网 IP 地址字符串。
        /// </summary>
        public static string GetInnerIP(this StartProcessConfig self, Fiber fiber)
        {
            return self.GetStartMachineConfig(fiber).InnerIP;
        }

        /// <summary>
        /// 获取当前进程所在机器的外网 IP 地址字符串。
        /// </summary>
        public static string GetOuterIP(this StartProcessConfig self, Fiber fiber)
        {
            return self.GetStartMachineConfig(fiber).OuterIP;
        }

        /// <summary>
        /// 获取当前进程的内网监听终结点（<c>InnerIP:Port</c>）。
        /// </summary>
        public static IPEndPoint GetInnerIPInnerPort(this StartProcessConfig self, Fiber fiber)
        {
            return NetworkHelper.ToIPEndPoint($"{self.GetInnerIP(fiber)}:{self.Port}");
        }

        /// <summary>
        /// 获取当前进程的内网通信 <see cref="Address"/> 结构（<c>InnerIP</c> + 进程内网端口）。
        /// </summary>
        public static Address GetAddress(this StartProcessConfig self, Fiber fiber)
        {
            return new Address(self.GetInnerIP(fiber), self.Port);
        }
    }
}
