using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// <see cref="AOIEntity"/> 生命周期系统，负责在组件创建与销毁时自动加入或移出 <see cref="AOIManagerComponent"/> 网格。
    /// </summary>
    [EntitySystemOf(typeof(AOIEntity))]
    public static partial class AOIEntitySystem2
    {
        /// <summary>
        /// 组件唤醒时读取父 <see cref="Unit"/> 的相位与位置，并将其注册到场景的 <see cref="AOIManagerComponent"/> 中。
        /// </summary>
        [EntitySystem]
        private static void Awake(this AOIEntity self)
        {
            Unit unit = self.GetParent<Unit>();
            NumericComponent numericComponent = unit.NumericComponent;
            self.Phase = (PhaseType)numericComponent.Get(NumericType.Phase);
            self.NumericComponent = numericComponent;
            self.Scene().GetComponent<AOIManagerComponent>().Add(self, unit.Position.x, unit.Position.z);
        }

        /// <summary>
        /// 组件销毁时自动从场景的 <see cref="AOIManagerComponent"/> 中注销移除。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this AOIEntity self)
        {
            Scene scene = self.Scene();
            if (scene.IsDisposed)
            {
                return;
            }
            scene.GetComponent<AOIManagerComponent>()?.Remove(self);
        }
    }

    /// <summary>
    /// <see cref="AOIEntity"/> 视野关系维护系统（负责九宫格 Cell 进出订阅、双向可见集合维护以及进出视野事件发布）。
    /// </summary>
    public static partial class AOIEntitySystem
    {
        /// <summary>
        /// 获取当前在我视野中的所有单位字典。
        /// </summary>
        // 获取在自己视野中的对象
        public static Dictionary<long, EntityRef<AOIEntity>> GetSeeUnits(this AOIEntity self)
        {
            return self.SeeUnits;
        }

        /// <summary>
        /// 获取当前能看见我的所有玩家单位字典（常用于将我的状态广播给周围玩家）。
        /// </summary>
        public static Dictionary<long, EntityRef<AOIEntity>> GetBeSeePlayers(this AOIEntity self)
        {
            return self.BeSeePlayers;
        }

        /// <summary>
        /// 获取当前在我视野中的所有玩家单位字典。
        /// </summary>
        public static Dictionary<long, EntityRef<AOIEntity>> GetSeePlayers(this AOIEntity self)
        {
            return self.SeePlayers;
        }

        /// <summary>
        /// 订阅指定 <see cref="Cell"/> 的进入事件，并将该网格内已有的所有单位加入我的视野。
        /// </summary>
        // cell中的unit进入self的视野
        public static void SubEnter(this AOIEntity self, Cell cell)
        {
            cell.SubsEnterEntities.Add(self.Id, self);
            foreach (KeyValuePair<long, EntityRef<AOIEntity>> kv in cell.AOIUnits)
            {
                if (kv.Key == self.Id)
                {
                    continue;
                }

                self.EnterSight(kv.Value);
            }
        }

        /// <summary>
        /// 取消订阅指定 <see cref="Cell"/> 的进入事件。
        /// </summary>
        public static void UnSubEnter(this AOIEntity self, Cell cell)
        {
            cell.SubsEnterEntities.Remove(self.Id);
        }

        /// <summary>
        /// 订阅指定 <see cref="Cell"/> 的离开事件（外圈防抖缓冲格）。
        /// </summary>
        public static void SubLeave(this AOIEntity self, Cell cell)
        {
            cell.SubsLeaveEntities.Add(self.Id, self);
        }

        /// <summary>
        /// 取消订阅指定 <see cref="Cell"/> 的离开事件，并将该网格内的所有单位移出我的视野。
        /// </summary>
        // cell中的unit离开self的视野
        public static void UnSubLeave(this AOIEntity self, Cell cell)
        {
            foreach (KeyValuePair<long, EntityRef<AOIEntity>> kv in cell.AOIUnits)
            {
                if (kv.Key == self.Id)
                {
                    continue;
                }

                self.LeaveSight(kv.Value);
            }

            cell.SubsLeaveEntities.Remove(self.Id);
        }
        
        /// <summary>
        /// 判断两个 <see cref="AOIEntity"/> 是否处于存在交集的同一相位（Phase）。
        /// </summary>
        public static bool SamePhase(this AOIEntity a, AOIEntity b)
        {
            return (a.Phase & b.Phase) != 0;
        }

        /// <summary>
        /// 当目标实体 <paramref name="enter"/> 进入 <paramref name="self"/> 的视野时，更新双向可见字典并发布 <see cref="UnitEnterSightRange"/> 事件。
        /// </summary>
        // enter进入self视野
        public static void EnterSight(this AOIEntity self, AOIEntity enter)
        {
            if (!self.SamePhase(enter))
            {
                return;
            }
            
            // 有可能之前在Enter，后来出了Enter还在LeaveCell，这样仍然没有删除，继续进来Enter，这种情况不需要处理
            if (self.SeeUnits.ContainsKey(enter.Id))
            {
                return;
            }
            
            if (!AOISeeCheckHelper.IsCanSee(self, enter))
            {
                return;
            }

            if (self.Unit.UnitType == UnitType.Player)
            {
                if (enter.Unit.UnitType == UnitType.Player)
                {
                    self.SeeUnits.Add(enter.Id, enter);
                    enter.BeSeeUnits.Add(self.Id, self);
                    self.SeePlayers.Add(enter.Id, enter);
                    enter.BeSeePlayers.Add(self.Id, self);
                    
                }
                else
                {
                    self.SeeUnits.Add(enter.Id, enter);
                    enter.BeSeeUnits.Add(self.Id, self);
                    enter.BeSeePlayers.Add(self.Id, self);
                }
            }
            else
            {
                if (enter.Unit.UnitType == UnitType.Player)
                {
                    self.SeeUnits.Add(enter.Id, enter);
                    enter.BeSeeUnits.Add(self.Id, self);
                    self.SeePlayers.Add(enter.Id, enter);
                }
                else
                {
                    self.SeeUnits.Add(enter.Id, enter);
                    enter.BeSeeUnits.Add(self.Id, self);
                }
            }
            EventSystem.Instance.Publish(self.Scene(), new UnitEnterSightRange() { A = self.Unit, B = enter.Unit });
        }

        /// <summary>
        /// 当目标实体 <paramref name="leave"/> 离开 <paramref name="self"/> 的视野时，从双向可见字典中移除并发布 <see cref="UnitLeaveSightRange"/> 事件。
        /// </summary>
        // leave离开self视野
        public static void LeaveSight(this AOIEntity self, AOIEntity leave)
        {
            if (self.Id == leave.Id)
            {
                return;
            }

            if (!self.SeeUnits.Remove(leave.Id))
            {
                return;
            }

            if (leave.Unit.UnitType == UnitType.Player)
            {
                self.SeePlayers.Remove(leave.Id);
            }

            leave.BeSeeUnits.Remove(self.Id);
            if (self.Unit.UnitType == UnitType.Player)
            {
                leave.BeSeePlayers.Remove(self.Id);
            }

            EventSystem.Instance.Publish(self.Scene(), new UnitLeaveSightRange { A = self.Unit, B = leave.Unit });
        }

        /// <summary>
        /// 是否在Unit视野范围内
        /// </summary>
        /// <param name="self"></param>
        /// <param name="unitId"></param>
        /// <returns></returns>
        public static bool IsBeSee(this AOIEntity self, long unitId)
        {
            return self.BeSeePlayers.ContainsKey(unitId);
        }
    }
}