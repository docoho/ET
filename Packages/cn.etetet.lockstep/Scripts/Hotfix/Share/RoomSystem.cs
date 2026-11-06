using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    /// <summary>
    /// 帧同步房间（<see cref="Room"/>）双端共享核心系统，负责房间初始化、单帧确定性世界推进、快照序列化/反序列化、状态哈希计算及录像数据记录
    /// </summary>
    [FriendOf(typeof(Room))]
    public static partial class RoomSystem
    {
        /// <summary>
        /// 获取当前实体所属的帧同步 <see cref="ET.Room"/> 容器
        /// </summary>
        public static Room Room(this Entity entity)
        {
            return entity.IScene as Room;
        }
        
        /// <summary>
        /// 初始化帧同步房间：设置开局时间、初始权威帧与预测帧、创建环形帧缓冲区 <see cref="FrameBuffer"/> 与固定时间计数器 <see cref="FixedTimeCounter"/>，并在 <see cref="LSWorld"/> 中生成所有初始战斗单位
        /// </summary>
        /// <param name="self">帧同步房间实例</param>
        /// <param name="unitInfos">所有参赛单位的初始信息列表</param>
        /// <param name="startTime">房间开局服务端毫秒时间戳</param>
        /// <param name="frame">起始帧号（正常开局为 -1，断线重连时为对应帧号）</param>
        public static void Init(this Room self, List<LockStepUnitInfo> unitInfos, long startTime, int frame = -1)
        {
            self.StartTime = startTime;
            self.AuthorityFrame = frame;
            self.PredictionFrame = frame;
            self.Replay.UnitInfos = unitInfos;
            self.FrameBuffer = new FrameBuffer(frame);
            self.FixedTimeCounter = new FixedTimeCounter(self.StartTime, 0, LSConstValue.UpdateInterval);
            LSWorld lsWorld = self.LSWorld;
            lsWorld.Frame = frame + 1;
            lsWorld.AddComponent<LSUnitComponent>();
            for (int i = 0; i < unitInfos.Count; ++i)
            {
                LockStepUnitInfo unitInfo = unitInfos[i];
                LSUnitFactory.Init(lsWorld, unitInfo);
                self.PlayerIds.Add(unitInfo.PlayerId);
            }
        }

        /// <summary>
        /// 使用指定帧的全员输入驱动帧同步房间推进一个逻辑帧：分发输入到各 <see cref="LSUnit"/>、保存当前帧世界快照与录像数据，并调用 <see cref="LSWorld.Update"/>
        /// </summary>
        /// <param name="self">帧同步房间实例</param>
        /// <param name="oneFrameInputs">当前帧的所有玩家输入</param>
        public static void Update(this Room self, OneFrameInputs oneFrameInputs)
        {
            LSWorld lsWorld = self.LSWorld;
            // 设置输入到每个LSUnit身上
            LSUnitComponent unitComponent = lsWorld.GetComponent<LSUnitComponent>();
            foreach (var kv in oneFrameInputs.Inputs)
            {
                LSUnit lsUnit = unitComponent.GetChild<LSUnit>(kv.Key);
                LSInputComponent lsInputComponent = lsUnit.GetComponent<LSInputComponent>();
                lsInputComponent.LSInput = kv.Value;
            }
            
            if (!self.IsReplay)
            {
                // 保存当前帧场景数据
                self.SaveLSWorld();
                self.Record(self.LSWorld.Frame);
            }

            lsWorld.Update();
        }
        
        /// <summary>
        /// 从环形帧缓冲区 <see cref="FrameBuffer"/> 中反序列化恢复指定历史帧的 <see cref="LSWorld"/> 实例（用于客户端预测回滚或哈希不一致排查）
        /// </summary>
        /// <param name="self">帧同步房间实例</param>
        /// <param name="sceneType">反序列化后赋予的 SceneType</param>
        /// <param name="frame">目标历史帧号</param>
        /// <returns>反序列化恢复出的帧同步世界实例</returns>
        public static LSWorld GetLSWorld(this Room self, int sceneType, int frame)
        {
            MemoryBuffer memoryBuffer = self.FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            LSWorld lsWorld = MemoryPackHelper.Deserialize(typeof (LSWorld), memoryBuffer) as LSWorld;
            lsWorld.SceneType = sceneType;
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            return lsWorld;
        }

        /// <summary>
        /// 将当前帧的 <see cref="LSWorld"/> 完整序列化写入 <see cref="FrameBuffer"/> 快照缓冲区，并计算字节流哈希值存入缓冲区
        /// </summary>
        /// <param name="self">帧同步房间实例</param>
        private static void SaveLSWorld(this Room self)
        {
            int frame = self.LSWorld.Frame;
            MemoryBuffer memoryBuffer = self.FrameBuffer.Snapshot(frame);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);
            
            MemoryPackHelper.Serialize(self.LSWorld, memoryBuffer);
            memoryBuffer.Seek(0, SeekOrigin.Begin);

            long hash = memoryBuffer.GetBuffer().Hash(0, (int) memoryBuffer.Length);
            
            self.FrameBuffer.SetHash(frame, hash);
        }

        // 记录需要存档的数据
        /// <summary>
        /// 将已确认的权威帧输入以及周期性世界快照（每 <see cref="LSConstValue.SaveLSWorldFrameCount"/> 帧一次）写入 <see cref="Replay"/> 对象以供录像回放
        /// </summary>
        /// <param name="self">帧同步房间实例</param>
        /// <param name="frame">要记录的帧号</param>
        public static void Record(this Room self, int frame)
        {
            if (frame > self.AuthorityFrame)
            {
                return;
            }
            OneFrameInputs oneFrameInputs = self.FrameBuffer.FrameInputs(frame);
            OneFrameInputs saveInput = OneFrameInputs.Create();
            oneFrameInputs.CopyTo(saveInput);
            self.Replay.FrameInputs.Add(saveInput);
            if (frame % LSConstValue.SaveLSWorldFrameCount == 0)
            {
                MemoryBuffer memoryBuffer = self.FrameBuffer.Snapshot(frame);
                byte[] bytes = memoryBuffer.ToArray();
                self.Replay.Snapshots.Add(bytes);
            }
        }
    }
}