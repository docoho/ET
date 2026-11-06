namespace ET.Server
{
    /// <summary>
    /// 服务端延长/缩短 Buff 持续时间节点处理器，支持固定值（Value）与百分比（Pct）调整并同步更新超时定时器。
    /// </summary>
    public class BTBuffAddExpireTimeHandler: ABTHandler<BTBuffAddExpireTime>
    {
        protected override int Run(BTBuffAddExpireTime node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            long leftTime = buff.ExpireTime - buff.CreateTime;
            leftTime = (int)((leftTime + node.Value) * (100 + node.Pct) / 100f);
            long expireTime = buff.CreateTime + leftTime;
            BuffHelper.UpdateExpireTime(buff, expireTime);
            return 0;
        }
    }
}