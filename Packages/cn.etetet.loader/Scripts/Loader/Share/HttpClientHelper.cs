using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 跨端 HTTP 客户端请求辅助类。
    /// 在 WebGL 平台下自动使用 <c>UnityWebRequest</c>，而在常规客户端/服务端（包括非主线程的 NetClient 纤程）下使用标准 .NET <see cref="HttpClient"/>。
    /// </summary>
    public static partial class HttpClientHelper
    {
        /// <summary>
        /// 异步发送 HTTP GET 请求并返回响应的字符串内容。
        /// </summary>
        /// <param name="link">请求的完整 URL 地址。</param>
        /// <returns>HTTP 响应正文字符串。</returns>
        public static async ETTask<string> Get(string link)
        {
            try
            {
#if UNITY_WEBGL // 这里只能限制WEBGL, 因为NetClient Fiber会调用，如果改成UNITY会导致不是在主线程使用
                UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(link);
                await req.SendWebRequest();
                return req.downloadHandler.text;
#else
                using HttpClient httpClient = new();
                HttpResponseMessage response =  await httpClient.GetAsync(link);
                string result = await response.Content.ReadAsStringAsync();
                return result;
#endif
            }
            catch (Exception e)
            {
                throw new Exception($"http request fail: {link.Substring(0,link.IndexOf('?'))}\n{e}");
            }
        }
    }
}