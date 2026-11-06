namespace ET.Server
{
    /// <summary>
    /// 服务端地图错误提示下发辅助类。
    /// </summary>
    public static class ErrorHelper
    {
        /// <summary>
        /// 向玩家单位所属的客户端发送带格式化参数的错误码消息（<see cref="M2C_Error"/>）。
        /// </summary>
        public static void MapError(Unit unit, int error, params string[] strs)
        {
            if (unit.UnitType != UnitType.Player)
            {
                return;
            }
            M2C_Error m2CError = M2C_Error.Create();
            m2CError.Error = error;
            m2CError.Values.AddRange(strs);
            MapMessageHelper.NoticeClient(unit, m2CError, NoticeType.Self);
        }
    }
}