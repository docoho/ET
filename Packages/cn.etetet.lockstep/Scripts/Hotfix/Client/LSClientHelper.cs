using System.IO;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端核心辅助工具类，封装预测回滚（Rollback）、状态哈希上报、其他玩家预测输入拷贝、录像保存与录像跳转逻辑
    /// </summary>
    public static partial class LSClientHelper
    {
        /// <summary>
        /// 递归遍历普通实体树（跳过 <see cref="LSEntity"/>），触发所有表现层实体的 <see cref="ILSRollbackSystem"/> 回滚修正回调
        /// </summary>
        /// <param name="entity">起始根实体（通常为 <see cref="Room"/>）</param>
        public static void RunLSRollbackSystem(Entity entity)
        {
            if (entity is LSEntity)
            {
                return;
            }
            
            LSEntitySystemSingleton.Instance.LSRollback(entity);
            
            if (entity.ComponentsCount() > 0)
            {
                foreach (var kv in entity.Components)
                {
                    RunLSRollbackSystem(kv.Value);
                }
            }

            if (entity.ChildrenCount() > 0)
            {
                foreach (var kv in entity.Children)
                {
                    RunLSRollbackSystem(kv.Value);
                }
            }
        }
        
        // 回滚
        /// <summary>
        /// 执行客户端帧同步预测回滚与重演（Rollback &amp; Resimulate）：
        /// <para>1. 从环形缓冲区快照恢复指定权威帧 <paramref name="frame"/> 的 <see cref="LSWorld"/> 状态；</para>
        /// <para>2. 使用服务端下发的真实权威帧输入推进 1 帧并上报状态哈希；</para>
        /// <para>3. 从 <c>AuthorityFrame + 1</c> 到当前 <c>PredictionFrame</c> 逐帧使用最新其他玩家输入重新预测模拟；</para>
        /// <para>4. 触发表现层 <see cref="RunLSRollbackSystem"/> 同步视图状态。</para>
        /// </summary>
        /// <param name="room">当前帧同步房间</param>
        /// <param name="frame">需要回滚到的权威帧号</param>
        public static void Rollback(Room room, int frame)
        {
            room.LSWorld.Dispose();
            FrameBuffer frameBuffer = room.FrameBuffer;
            
            // 回滚
            room.LSWorld = room.GetLSWorld(SceneType.LockStepClient, frame);
            OneFrameInputs authorityFrameInput = frameBuffer.FrameInputs(frame);
            // 执行AuthorityFrame
            room.Update(authorityFrameInput);
            room.SendHash(frame);

            
            // 重新执行预测的帧
            for (int i = room.AuthorityFrame + 1; i <= room.PredictionFrame; ++i)
            {
                OneFrameInputs oneFrameInputs = frameBuffer.FrameInputs(i);
                LSClientHelper.CopyOtherInputsTo(room, authorityFrameInput, oneFrameInputs); // 重新预测消息
                room.Update(oneFrameInputs);
            }
            
            RunLSRollbackSystem(room);
        }
        
        /// <summary>
        /// 向服务端房间上报指定权威帧的本地世界快照哈希值，用于检测两端是否发生不同步（Desync）
        /// </summary>
        /// <param name="self">当前帧同步房间</param>
        /// <param name="frame">要校验的帧号</param>
        public static void SendHash(this Room self, int frame)
        {
            if (frame > self.AuthorityFrame)
            {
                return;
            }
            long hash = self.FrameBuffer.GetHash(frame);
            C2Room_CheckHash c2RoomCheckHash = C2Room_CheckHash.Create();
            c2RoomCheckHash.Frame = frame;
            c2RoomCheckHash.Hash = hash;
            self.Root().GetComponent<ClientSenderComponent>().Send(c2RoomCheckHash);
        }
        
        // 重新调整预测消息，只需要调整其他玩家的输入
        /// <summary>
        /// 将权威帧中除本地玩家以外的其他玩家输入复制到后续预测帧中（本地玩家保留自己在预测帧时的真实操作）
        /// </summary>
        /// <param name="room">当前帧同步房间</param>
        /// <param name="from">源权威帧输入</param>
        /// <param name="to">目标预测帧输入</param>
        public static void CopyOtherInputsTo(Room room, OneFrameInputs from, OneFrameInputs to)
        {
            long myId = room.GetComponent<LSClientUpdater>().MyId;
            foreach (var kv in from.Inputs)
            {
                if (kv.Key == myId)
                {
                    continue;
                }
                to.Inputs[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 将当前对局的完整帧输入与周期性世界快照序列化保存为本地录像文件
        /// </summary>
        /// <param name="room">当前帧同步房间</param>
        /// <param name="path">录像文件保存路径</param>
        public static void SaveReplay(Room room, string path)
        {
            if (room.IsReplay)
            {
                return;
            }
            
            Log.Debug($"save replay: {path} frame: {room.Replay.FrameInputs.Count}");
            byte[] bytes = MemoryPackHelper.Serialize(room.Replay);
            File.WriteAllBytes(path, bytes);
        }
        
        /// <summary>
        /// 在录像回放模式下跳转到指定目标帧（利用最近的关键帧快照反序列化恢复并重设时间计数器，从而实现快速拖拽跳转）
        /// </summary>
        /// <param name="room">回放房间实例</param>
        /// <param name="frame">目标跳转帧号</param>
        public static void JumpReplay(Room room, int frame)
        {
            if (!room.IsReplay)
            {
                return;
            }

            if (frame >= room.Replay.FrameInputs.Count)
            {
                frame = room.Replay.FrameInputs.Count - 1;
            }
            
            int snapshotIndex = frame / LSConstValue.SaveLSWorldFrameCount;
            Log.Debug($"jump replay start {room.AuthorityFrame} {frame} {snapshotIndex}");
            if (snapshotIndex != room.AuthorityFrame / LSConstValue.SaveLSWorldFrameCount || frame < room.AuthorityFrame)
            {
                room.LSWorld.Dispose();
                // 回滚
                byte[] memoryBuffer = room.Replay.Snapshots[snapshotIndex];
                LSWorld lsWorld = MemoryPackHelper.Deserialize(typeof (LSWorld), memoryBuffer, 0, memoryBuffer.Length) as LSWorld;
                room.LSWorld = lsWorld;
                room.AuthorityFrame = snapshotIndex * LSConstValue.SaveLSWorldFrameCount;
                RunLSRollbackSystem(room);
            }
            
            room.FixedTimeCounter.Reset(room.GetSingleton<TimeInfo>().ServerNow() - frame * LSConstValue.UpdateInterval, 0);

            Log.Debug($"jump replay finish {frame}");
        }
    }
}