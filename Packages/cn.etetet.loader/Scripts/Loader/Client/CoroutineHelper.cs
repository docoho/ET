using System;
using UnityEngine;
using UnityEngine.Networking;

namespace ET
{
    /// <summary>
    /// Unity 异步操作（<see cref="AsyncOperation"/>）与 <see cref="ETTask"/> 协程桥接辅助类。
    /// </summary>
    public static class CoroutineHelper
    {
        // 有了这个方法，就可以直接await Unity的AsyncOperation了
        /// <summary>
        /// 为 Unity 的 <see cref="AsyncOperation"/> 提供自定义 <c>GetAwaiter</c> 扩展方法，使业务代码可以直接使用 <c>await asyncOperation;</c> 等待场景加载或 Web 请求完成。
        /// </summary>
        /// <param name="asyncOperation">Unity 异步操作实例。</param>
        public static async ETTask GetAwaiter(this AsyncOperation asyncOperation)
        {
            ETTask task = ETTask.Create(true);
            asyncOperation.completed += _ => { task.SetResult(); };
            await task;
        }
        
        /// <summary>
        /// 使用 <see cref="UnityWebRequest"/> 发起异步 HTTP GET 请求并返回响应文本。
        /// </summary>
        /// <param name="link">请求的 URL 链接。</param>
        /// <returns>HTTP 响应正文字符串。</returns>
        public static async ETTask<string> HttpGet(string link)
        {
            try
            {
                UnityWebRequest req = UnityWebRequest.Get(link);
                await req.SendWebRequest();
                return req.downloadHandler.text;
            }
            catch (Exception e)
            {
                throw new Exception($"http request fail: {link.Substring(0,link.IndexOf('?'))}\n{e}");
            }
        }
    }
}