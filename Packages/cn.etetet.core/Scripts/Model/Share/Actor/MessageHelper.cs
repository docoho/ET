using System;

namespace ET
{
    /// <summary>
    /// Actor 消息辅助工具类。
    /// </summary>
    public static class MessageHelper
    {
        /// <summary>
        /// 根据请求消息类型 <paramref name="requestType"/> 查找配对的响应类型，从对象池创建响应实例并填入 <paramref name="rpcId"/> 与 <paramref name="error"/> 错误码。
        /// </summary>
        public static IResponse CreateResponse(Type requestType, int rpcId, int error)
        {
            Type responseType = OpcodeType.Instance.GetResponseType(requestType);
            IResponse response = (IResponse)ObjectPool.Fetch(responseType);
            response.Error = error;
            response.RpcId = rpcId;
            return response;
        }
    }
}