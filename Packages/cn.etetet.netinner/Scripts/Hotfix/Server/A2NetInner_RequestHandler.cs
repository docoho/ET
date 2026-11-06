using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 本进程其他 Fiber 发往 <c>NetInner</c> 纤程的跨进程 RPC 请求代理处理器。
    /// <para>接收进程内 <see cref="A2NetInner_Request"/>，在 <c>NetInner</c> 纤程上通过 <see cref="ProcessOuterSender"/> 向目标远端进程发起异步网络 RPC，并将远端响应包装为 <see cref="A2NetInner_Response"/> 返回给发起方 Fiber。</para>
    /// </summary>
    [MessageHandler(SceneType.NetInner)]
    public class A2NetInner_RequestHandler: MessageHandler<Scene, A2NetInner_Request, A2NetInner_Response>
    {
        protected override async ETTask Run(Scene root, A2NetInner_Request request, A2NetInner_Response response)
        {
            int rpcId = request.RpcId;
            IResponse res = await root.GetComponent<ProcessOuterSender>().Call(request.ActorId, request.MessageObject, false);
            res.RpcId = rpcId;
            response.MessageObject = res;
        }
    }
}