using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 纤程内部的 Actor 消息邮箱（<c>MailBoxComponent</c>）索引表。
    /// <para>
    /// 【工作原理】
    /// 任何实体只要挂载了 <c>MailBoxComponent</c>，就会将自己注册到所在 <see cref="Fiber.Mailboxes"/> 中（以宿主父实体的 <see cref="Entity.InstanceId"/> 为 Key）。
    /// 当收到发往本纤程某个 <c>InstanceId</c> 的 Actor 消息时，直接通过 <see cref="Get"/> 即可 O(1) 查找到对应的 <c>MailBoxComponent</c> 进行消息分发！
    /// </para>
    /// </summary>
    public class Mailboxes
    {
        /// <summary>Key 为挂载了 MailBoxComponent 的父实体 InstanceId，Value 为 MailBoxComponent 实体的安全引用</summary>
        private readonly Dictionary<long, EntityRef<Entity>> mailboxes = new();
        
        /// <summary>
        /// 注册一个 <c>MailBoxComponent</c>（以 <c>mailBox.Parent.InstanceId</c> 作为寻址 Key）。
        /// </summary>
        public void Add(Entity mailBox)
        {
            this.mailboxes.Add(mailBox.Parent.InstanceId, mailBox);
        }
        
        /// <summary>
        /// 当 <c>MailBoxComponent</c> 销毁时注销对应的 <paramref name="instanceId"/>。
        /// </summary>
        public void Remove(long instanceId)
        {
            this.mailboxes.Remove(instanceId);
        }

        /// <summary>
        /// 根据目标实体的 <paramref name="instanceId"/> 查找其挂载的 <c>MailBoxComponent</c>。
        /// </summary>
        public EntityRef<Entity> Get(long instanceId)
        {
            this.mailboxes.TryGetValue(instanceId, out EntityRef<Entity> entity);
            return entity;
        }
    }
}