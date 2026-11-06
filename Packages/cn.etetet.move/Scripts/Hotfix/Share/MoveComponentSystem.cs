using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 移动组件逻辑系统 (MoveComponentSystem)
    /// <para>【初学者导读】：</para>
    /// <para>1. 本系统负责处理 Unit（如角色、怪物体）在空间中的平滑路径移动与转向插值。</para>
    /// <para>2. 核心原理：基于“真实时间戳”与“路径线段”，通过线性插值 (Lerp) 计算位置，通过球面插值 (Slerp) 计算朝向。</para>
    /// <para>3. 时间驱动机制：没有直接使用 Mono 的 Update，而是通过 ET 的 <see cref="TimerComponent.NewFrameTimer"/> 每帧驱动。</para>
    /// <para>4. 掉帧平滑补偿：帧间隔较大（如卡顿）跨越多个路标点时，通过内部的 while 循环持续结算，确保位移精确不丢步。</para>
    /// </summary>
    [EntitySystemOf(typeof(MoveComponent))]
    public static partial class MoveComponentSystem
    {
        /// <summary>
        /// 每帧移动定时器回调
        /// <para>向 ET 的 TimerComponent 注册，通过计时器每帧触发一次 Run 方法，驱动角色前移。</para>
        /// </summary>
        [Invoke(TimerInvokeType.MoveTimer)]
        public class MoveTimer: ATimer<MoveComponent>
        {
            protected override void Run(MoveComponent self)
            {
                try
                {
                    // 驱动前进一步；参数 true 表示如果到达终点，对外通知正常完成
                    self.MoveForward(true);
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }
        
        /// <summary>
        /// 组件销毁时的生命周期处理
        /// </summary>
        [EntitySystem]
        private static void Destroy(this MoveComponent self)
        {
            // 实体销毁时强制结束移动，传入 false 表示本次移动未正常完成
            self.MoveFinish(false);
        }
        
        /// <summary>
        /// 组件创建/唤醒时的生命周期处理，负责重置所有移动相关的状态字段
        /// </summary>
        [EntitySystem]
        private static void Awake(this MoveComponent self)
        {
            self.StartTime = 0;
            self.StartPos = float3.zero;
            self.NeedTime = 0;
            self.MoveTimer = 0;
            self.tcs = null;
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
        }
        
        /// <summary>
        /// 判断当前是否已经到达目的地
        /// </summary>
        /// <returns>若目标路点列表为空，则说明当前没有移动任务或已抵达终点</returns>
        public static bool IsArrived(this MoveComponent self)
        {
            return self.Targets.Count == 0;
        }

        /// <summary>
        /// 在移动过程中动态改变移动速度
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="speed">新的速度（米/秒）</param>
        /// <returns>速度是否修改成功</returns>
        public static bool ChangeSpeed(this MoveComponent self, float speed)
        {
            // 如果已经到达或没有在移动，不能更改速度
            if (self.IsArrived())
            {
                return false;
            }

            // 速度必须大于 0
            if (speed < 0.0001)
            {
                return false;
            }
            
            Unit unit = self.GetParent<Unit>();

            // 使用对象池租借一个临时列表，避免每次变速产生 GC
            using ListComponent<float3> path = ListComponent<float3>.Create();
            
            // 1. 先将角色位置即时结算到当前真实时刻，保证位置连续不跳变
            self.MoveForward(false);
                
            // 2. 以角色当前瞬时位置作为新路径的起点
            path.Add(unit.Position);
            
            // 3. 将后续尚未走完的目标路径点依次添加进来
            for (int i = self.N; i < self.Targets.Count; ++i)
            {
                path.Add(self.Targets[i]);
            }
            
            // 4. 以新速度重新开启移动协程（Coroutine() 表示后台独立跑，不阻塞当前函数）
            self.MoveToAsync(path, speed).Coroutine();
            return true;
        }

        /// <summary>
        /// 异步移动到指定路径点序列（协程方法）
        /// <para>【初学者注意】：</para>
        /// <para>- 上层逻辑调用后可以 `await MoveToAsync(...)`，直到移动到达终点或被取消。</para>
        /// <para>- 如果上一次移动还没完成，再次调用会自动打断上一次移动并开启新移动。</para>
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="target">完整的路标点列表（包含各拐点坐标）</param>
        /// <param name="speed">移动速度（米/秒）</param>
        /// <param name="turnTime">转向平滑时间（毫秒），在该时间内完成朝向插值</param>
        /// <returns>是否正常移动到目的地（true 表示正常到达，false 表示被中途打断/停止）</returns>
        public static async ETTask<bool> MoveToAsync(this MoveComponent self, List<float3> target, float speed, int turnTime = 100)
        {
            // 1. 停止上一次可能正在进行的移动（传入 false 表示前一次移动被中断）
            self.Stop(false);

            // 2. 拷贝目标路点列表
            foreach (float3 v in target)
            {
                self.Targets.Add(v);
            }

            // 3. 配置移动参数
            self.IsTurnHorizontal = true; // 仅水平转向（Yaw 轴旋转），防止角色俯仰
            self.TurnTime = turnTime;     // 转向平滑时间
            self.Speed = speed;           // 移动速度
            self.tcs = ETTask<bool>.Create(true); // 创建异步等待任务源（TaskCompletionSource）

            // 4. 派发移动开始事件（例如通知表现层播放奔跑动画、发送网络同步包等）
            EventSystem.Instance.Publish(self.Scene(), new MoveStart() {Unit = self.GetParent<Unit>()});
            
            // 5. 开启移动定时器
            self.StartMove();

            // 【ET 核心安全机制】：
            // 在 await 异步等待期间，Entity 可能会被销毁（例如角色下线、场景切换等）。
            // 使用 EntityRef 保存实体软引用，防止 await 恢复后访问到已失效的野指针。
            EntityRef<MoveComponent> selfRef = self;
            
            // 等待移动完成或被外部 Stop 打断
            bool moveRet = await self.tcs;

            // 6. 如果是正常移动到终点，派发停止移动事件（例如切回站立待机动画）
            if (moveRet)
            {
                self = selfRef;
                EventSystem.Instance.Publish(self.Scene(), new MoveStop() {Unit = self.GetParent<Unit>()});
            }
            return moveRet;
        }

        /// <summary>
        /// 核心移动推进逻辑（按时间戳插值计算当前位置与朝向）
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="ret">当移动在此次推进中结束时，返回给外部异步等待协程的结果（true: 正常到达, false: 中断）</param>
        private static void MoveForward(this MoveComponent self, bool ret)
        {
            Unit unit = self.GetParent<Unit>();
            
            // 获取当前客户端时间戳（毫秒）
            long timeNow = self.GetSingleton<TimeInfo>().ClientNow();
            // 计算自当前路段起点开始，已经经过的时间
            long moveTime = timeNow - self.StartTime;

            // 循环处理路段，避免帧率波动或卡顿导致单帧移动距离超过一个线段长度
            while (true)
            {
                // 如果经过时间 <= 0，说明还没到移动开始时间，直接返回
                if (moveTime <= 0)
                {
                    return;
                }
                
                // 【情况 1】：经过的时间已经超过了当前路段所需的时间
                if (moveTime >= self.NeedTime)
                {
                    // 直接将位置与朝向拉到当前路段的终点
                    unit.Position = self.NextTarget;
                    if (self.TurnTime > 0)
                    {
                        unit.Rotation = self.To;
                    }
                }
                // 【情况 2】：仍在当前路段中间，进行平滑插值
                else
                {
                    // 计算位置插值进度 (0.0 ~ 1.0)
                    float amount = moveTime * 1f / self.NeedTime;
                    if (amount > 0)
                    {
                        // 线性插值 (Lerp)：从起点移动到下一个目标点
                        float3 newPos = math.lerp(self.StartPos, self.NextTarget, amount);
                        unit.Position = newPos;
                    }
                    
                    // 计算转向插值
                    if (self.TurnTime > 0)
                    {
                        // 计算转向插值进度 (0.0 ~ 1.0)
                        amount = moveTime * 1f / self.TurnTime;
                        if (amount > 1)
                        {
                            amount = 1f;
                        }
                        // 球面插值 (Slerp)：平滑旋转四元数
                        quaternion q = math.slerp(self.From, self.To, amount);
                        unit.Rotation = q;
                    }
                }

                // 减去当前路段已消耗的时间
                moveTime -= self.NeedTime;

                // 若剩余时间 < 0，说明当前路段还未走完，等待下一帧继续插值
                if (moveTime < 0)
                {
                    return;
                }
                
                // 运行到此处，说明当前路段已完全走完
                
                // 检查是否已经抵达整条路径的最后一个点
                if (self.N >= self.Targets.Count - 1)
                {
                    unit.Position = self.NextTarget;
                    if (self.TurnTime > 0)
                    {
                        unit.Rotation = self.To;
                    }

                    // 移动全部完成，触发结束清理
                    self.MoveFinish(ret);
                    return;
                }

                // 尚未到达终点，切换到下一个路标点，继续进入下一个循环推进剩余的 moveTime
                self.SetNextTarget();
            }
        }

        /// <summary>
        /// 启动移动过程
        /// <para>记录初始时间，切换首个路段，并向定时器系统注册每帧帧回调</para>
        /// </summary>
        private static void StartMove(this MoveComponent self)
        {
            // 记录开始移动时的初始时间戳
            self.BeginTime = self.GetSingleton<TimeInfo>().ClientNow();
            self.StartTime = self.BeginTime;
            
            // 准备第一个目标路段
            self.SetNextTarget();

            // 注册每帧调用的 FrameTimer（下一帧起每帧执行 MoveTimer）
            self.MoveTimer = self.Root().TimerComponent.NewFrameTimer(TimerInvokeType.MoveTimer, self);
        }

        /// <summary>
        /// 切换并准备下一个目标路标点
        /// <para>负责计算下一个线段的距离、所需耗时以及转向目标四元数</para>
        /// </summary>
        private static void SetNextTarget(this MoveComponent self)
        {
            Unit unit = self.GetParent<Unit>();

            // 推进到下一个点索引
            ++self.N;

            // 计算当前线段的位移向量及距离 (米)
            float3 v = self.GetFaceV();
            float distance = math.length(v);
            
            // 插值的起始点以 Unit 当前的实际物理位置为准
            self.StartPos = unit.Position;

            // 更新本路段的理论起始时间（累加上一段的需求耗时，确保时间连续）
            self.StartTime += self.NeedTime;
            
            // 根据公式：时间(毫秒) = 距离 / 速度 * 1000
            self.NeedTime = (long) (distance / self.Speed * 1000);
            
            // 如果设置了平滑转向时间，计算朝向四元数
            if (self.TurnTime > 0)
            {
                float3 faceV = self.GetFaceV();
                // 距离过短无需转向，防止除零或朝向突变
                if (math.lengthsq(faceV) < 0.0001f)
                {
                    return;
                }
                
                // 起始朝向为当前角色的朝向
                self.From = unit.Rotation;
                
                // 如果只进行水平转向，将高度差分量 Y 归零
                if (self.IsTurnHorizontal)
                {
                    faceV.y = 0;
                }

                // 当 X 或 Z 轴有明显的移动分量时，计算目标朝向四元数
                if (Math.Abs(faceV.x) > 0.01 || Math.Abs(faceV.z) > 0.01)
                {
                    self.To = quaternion.LookRotation(faceV, math.up());
                }

                return;
            }
        }

        /// <summary>
        /// 获取当前行进线段的方向向量 (从上一个路点指向当前目标路点)
        /// </summary>
        private static float3 GetFaceV(this MoveComponent self)
        {
            return self.NextTarget - self.PreTarget;
        }

        /// <summary>
        /// 瞬间移动（闪现）到目标位置
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="target">瞬移目的坐标</param>
        /// <returns>操作是否成功</returns>
        public static bool FlashTo(this MoveComponent self, float3 target)
        {
            Unit unit = self.GetParent<Unit>();
            unit.Position = target;
            return true;
        }

        /// <summary>
        /// 外部主动停止移动
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="ret">返回给等待协程的布尔结果（通常传入 false 表示被强制打断）</param>
        public static void Stop(this MoveComponent self, bool ret)
        {
            // 如果还有路径未走完，先推进结算到当前瞬间的位置，避免位置回退
            if (self.Targets.Count > 0)
            {
                self.MoveForward(ret);
            }

            // 彻底清理移动状态并唤醒等待的协程
            self.MoveFinish(ret);
        }

        /// <summary>
        /// 移动结束清理（复位所有参数、注销定时器并触发协程回调）
        /// </summary>
        /// <param name="self">移动组件</param>
        /// <param name="ret">返回给 MoveToAsync 异步协程的结果</param>
        private static void MoveFinish(this MoveComponent self, bool ret)
        {
            // 如果 StartTime == 0 说明当前并未处于移动状态，无需重复清理
            if (self.StartTime == 0)
            {
                return;
            }
            
            // 重置所有移动状态数据
            self.StartTime = 0;
            self.StartPos = float3.zero;
            self.BeginTime = 0;
            self.NeedTime = 0;
            self.Targets.Clear();
            self.Speed = 0;
            self.N = 0;
            self.TurnTime = 0;
            self.IsTurnHorizontal = false;
            
            // 注销每帧驱动定时器
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent?.Remove(ref self.MoveTimer);    
            }

            // 唤醒并结束 MoveToAsync 的异步等待
            if (self.tcs != null)
            {
                var tcs = self.tcs;
                self.tcs = null;
                tcs.SetResult(ret);
            }
        }
    }
}
