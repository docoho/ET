namespace ET.Client
{
    /// <summary>
    /// 客户端技能施放发送辅助类。
    /// </summary>
    public static class SpellHelper
    {
        /// <summary>
        /// 向服务端发送施放技能消息（<see cref="C2M_SpellCast"/>）。
        /// </summary>
        public static void Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
        }
    }
}
