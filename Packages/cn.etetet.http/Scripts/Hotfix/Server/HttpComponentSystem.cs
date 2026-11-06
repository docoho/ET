using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 监听组件（<see cref="HttpComponent"/>）的生命周期与请求分发系统类。
    /// <para>基于 .NET <see cref="HttpListener"/> 监听指定的 URL 前缀（支持分号分隔多个地址），接收 HTTP 请求后根据当前 <c>SceneType</c> 与请求路径（<c>AbsolutePath</c>）交由 <see cref="HttpDispatcher"/> 路由到对应的 <see cref="IHttpHandler"/> 处理。</para>
    /// </summary>
    [EntitySystemOf(typeof(HttpComponent))]
    public static partial class HttpComponentSystem
    {
        /// <summary>
        /// 初始化并启动 <see cref="HttpListener"/>，开始异步循环接收 HTTP 请求。
        /// </summary>
        /// <param name="self">HTTP 组件实例。</param>
        /// <param name="address">以分号 <c>;</c> 分隔的监听地址前缀列表（如 <c>http://127.0.0.1:8080/;</c>）。</param>
        [EntitySystem]
        private static void Awake(this HttpComponent self, string address)
        {
            try
            {
                self.Listener = new HttpListener();

                foreach (string s in address.Split(';'))
                {
                    if (s.Trim() == "")
                    {
                        continue;
                    }
                    self.Listener.Prefixes.Add(s);
                }

                self.Listener.Start();

                self.Accept().Coroutine();
            }
            catch (HttpListenerException e)
            {
                throw new Exception($"请先在cmd中运行: netsh http add urlacl url=http://*:你的address中的端口/ user=Everyone, address: {address}", e);
            }
        }
        
        [EntitySystem]
        private static void Destroy(this HttpComponent self)
        {
            if (self.Listener == null)
            {
                return;
            }
            try
            {
                if (self.Listener.IsListening)
                {
                    self.Listener.Stop();
                }
            }
            catch (Exception)
            {
                // ignored
            }
            
            try
            {
                self.Listener.Close();
            }
            catch (ObjectDisposedException)
            {
                // HttpListener已经被释放，忽略异常
            }
            finally
            {
                self.Listener = null;
            }
        }

        private static async ETTask Accept(this HttpComponent self)
        {
            EntityRef<HttpComponent> selfRef = self;
            while (true)
            {
                try
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    
                    if (self.Listener == null)
                    {
                        return;
                    }

                    if (!self.Listener.IsListening)
                    {
                        return;
                    }

                    HttpListenerContext context = await self.Listener.GetContextAsync();
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    self.Handle(context).Coroutine();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async ETTask Handle(this HttpComponent self, HttpListenerContext context)
        {
            try
            {
                IHttpHandler handler = HttpDispatcher.Instance.Get(self.IScene.SceneType, context.Request.Url.AbsolutePath);
                await handler.Handle(self.Scene(), context);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                context.Response.Close();    
            }
            
        }
    }
}