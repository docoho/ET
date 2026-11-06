namespace ET.Client
{
    /// <summary>
    /// NetClient 独立网络纤程处理主纤程委托发送的 RPC 请求（A2NetClient_Request），调用网关 Session.Call 并将响应返回给主纤程。
    /// </summary>
    [MessageHandler(SceneType.NetClient)]
    public class A2NetClient_RequestHandler: MessageHandler<Scene, A2NetClient_Request, A2NetClient_Response>
    {
        protected override async ETTask Run(Scene root, A2NetClient_Request request, A2NetClient_Response response)
        {
            int rpcId = request.RpcId;
            IResponse res = await root.GetComponent<SessionComponent>().Session.Call(request.MessageObject);
            res.RpcId = rpcId;
            response.MessageObject = res;
        }
    }
}