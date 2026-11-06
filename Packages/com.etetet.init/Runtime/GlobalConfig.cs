using UnityEngine;

namespace ET
{
    /// <summary>
    /// 代码运行模式枚举，决定了 Unity 与命令行构建时哪些代码目录（Client/Server/Share）通过 <c>.asmref</c> 参与编译。
    /// </summary>
    public enum CodeMode
    {
        /// <summary>纯客户端模式：仅编译客户端代码（Client + Share），用于客户端正式打包发布。</summary>
        Client = 1,
        /// <summary>纯服务端模式：仅编译服务端代码（Server + Share）。</summary>
        Server = 2,
        /// <summary>双端同进程开发模式：同时编译客户端与服务端代码（Client + Server + Share + Test），支持在 Unity 编辑器内直接单进程联调。</summary>
        ClientServer = 3,
    }
    
    /// <summary>
    /// 构建类型枚举（调试版或发布版）。
    /// </summary>
    public enum BuildType
    {
        /// <summary>调试构建模式，包含调试符号与开发期检查。</summary>
        Debug,
        /// <summary>正式发布构建模式，开启编译优化。</summary>
        Release,
    }
    
    /// <summary>
    /// ET 全局初始化配置资产（<see cref="ScriptableObject"/>），存放于 <c>Resources/GlobalConfig.asset</c>。
    /// 用于在编辑器和运行时指定当前项目的运行模式（<see cref="CodeMode"/>）、启动主场景名、服务器连接地址等核心参数。
    /// </summary>
    [CreateAssetMenu(menuName = "ET/CreateGlobalConfig", fileName = "GlobalConfig", order = 0)]
    public class GlobalConfig: ScriptableObject
    {
        /// <summary>当前工程的代码编译与运行模式（Client / Server / ClientServer）。</summary>
        public CodeMode CodeMode;

        /// <summary>启动入口主包/场景名称（如 <c>AllServer</c>、<c>StateSync</c>、<c>LockStep</c> 等，对应 <c>cn.etetet.{scenename}</c> 主包）。</summary>
        public string SceneName;

        /// <summary>客户端连接的远端服务器（Router/Realm）IP 与端口地址（格式如 <c>127.0.0.1:10002</c>）。</summary>
        public string Address;

        /// <summary>是否启用编辑器脚本编译模式（不走热更 DLL 加载而直接使用编辑器编译的程序集）。</summary>
        public bool EditorScripts;
    }
}
