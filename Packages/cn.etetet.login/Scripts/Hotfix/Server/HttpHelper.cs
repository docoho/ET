using System.Net;
using System.Text;

namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 响应辅助工具类。
    /// </summary>
    public static partial class HttpHelper
    {
        /// <summary>
        /// 将响应对象序列化为 UTF-8 JSON 字符串并写入 HttpListenerContext 的响应输出流（状态码 200）。
        /// </summary>
        public static void Response(HttpListenerContext context, object response)
        {
            byte[] bytes = MongoHelper.ToJson(response).ToUtf8();
            context.Response.StatusCode = 200;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }
}