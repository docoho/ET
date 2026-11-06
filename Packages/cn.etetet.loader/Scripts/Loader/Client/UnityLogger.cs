using System;
using System.Text.RegularExpressions;

namespace ET
{
    /// <summary>
    /// Unity 环境下的日志创建回调处理器：响应 <see cref="LogInvoker"/> 请求，为每个纤程创建带有对应场景名称前缀的 <see cref="UnityLogger"/> 实例。
    /// </summary>
    [Invoke]
    public class LogInvoker_Unity: AInvokeHandler<LogInvoker, ILog>
    {
        public override ILog Handle(LogInvoker args)
        {
            return new UnityLogger(args.SceneName);
        }
    }
    
    /// <summary>
    /// 基于 <see cref="UnityEngine.Debug"/> 的客户端/编辑器日志实现类。
    /// 在 Unity 编辑器下支持将异常堆栈中的文件与行号转换为可点击跳转的超链接格式（<c>&lt;a href="..." line="..."&gt;</c>）。
    /// </summary>
    public class UnityLogger: ILog
    {
        /// <summary>当前日志所属的纤程/场景名称前缀。</summary>
        private string sceneName;

        public UnityLogger(string sceneName)
        {
            this.sceneName = sceneName;
        }

        public string SceneName { set => sceneName = value; }

        public void Trace(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Debug(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Info(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning($"{this.sceneName} {msg}");
        }

        public void Error(string msg)
        {
#if UNITY_EDITOR
            msg = Msg2LinkStackMsg(msg);
#endif
            UnityEngine.Debug.LogError(msg);
        }
        
        /// <summary>
        /// 将堆栈字符串中的 <c>at Method in File.cs:line</c> 替换为 Unity Console 支持点击跳转的 HTML 超链接标签。
        /// </summary>
        private static string Msg2LinkStackMsg(string msg)
        {
            msg = Regex.Replace(msg,@"at (.*?) in (.*?\.cs):(\w+)", match =>
            {
                string path = match.Groups[2].Value;
                string line = match.Groups[3].Value;
                return $"{match.Groups[1].Value}\n<a href=\"{path}\" line=\"{line}\">{path}:{line}</a>";
            });
            return msg;
        }

        public void Error(Exception e)
        {
            string msg = e.ToString();
#if UNITY_EDITOR
            msg = Msg2LinkStackMsg(msg);
#endif
            UnityEngine.Debug.LogError($"{this.sceneName,-10}\n{msg}");
        }
    }
}