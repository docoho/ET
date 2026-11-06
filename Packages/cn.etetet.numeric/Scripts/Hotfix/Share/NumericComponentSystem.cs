namespace ET
{
    /// <summary>
    /// 数值组件系统（管理单位的所有属性字典、五元组公式重算、一级属性对二级属性的联动影响以及数值变化事件派发）。
    /// <remarks>
    /// 核心设计：
    /// 1. 所有数值底层统一以 <c>long</c> 整型存储在 <see cref="NumericComponent.NumericDic"/> 中，浮点数通过乘以 1000（定点数思想）存储；
    /// 2. 枚举值 &lt; <see cref="Max"/>（10000）的是一级最终属性（如 <c>Speed = 1000</c>、<c>MaxHP = 1002</c>）；
    /// 3. 枚举值 &gt;= <see cref="Max"/> 的是二级属性分量，遵循五元组合成公式：
    ///    <c>final = (((Base + Add) * (100 + Pct) / 100) + FinalAdd) * (100 + FinalPct) / 100</c>；
    /// 4. 修改二级属性分量会自动触发 <see cref="Update(NumericComponent,NumericType,bool)"/> 重算对应的一级最终属性，并在一级属性变化时发布 <see cref="NumbericChange"/> 事件。
    /// </remarks>
    /// </summary>
    [EntitySystemOf(typeof(NumericComponent))]
    public static partial class NumericComponentSystem
    {
        /// <summary>
        /// 组件唤醒时将自身引用绑定到父实体 <see cref="Unit.NumericComponent"/> 快捷属性上。
        /// </summary>
        [EntitySystem]
        private static void Awake(this NumericComponent self)
        {
            self.GetParent<Unit>().NumericComponent = self;
        }
        
        /// <summary>
        /// 反序列化（如跨进程传送恢复）时重新将自身引用绑定到父实体 <see cref="Unit.NumericComponent"/> 上。
        /// </summary>
        [EntitySystem]
        private static void Deserialize(this NumericComponent self)
        {
            self.GetParent<Unit>().NumericComponent = self;
        }

        /// <summary>
        /// 一级最终属性与二级属性分量的分界阈值（小于 10000 为最终属性，大于等于 10000 为五元组子属性）。
        /// </summary>
        public const int Max = 10000;

        /// <summary>
        /// 将当前数值组件中的所有键值对浅拷贝到目标数值组件中。
        /// </summary>
        public static void CopyTo(this NumericComponent self, NumericComponent to)
        {
            foreach (var kv in self.NumericDic)
            {
                to.NumericDic[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 按整型属性 ID 获取浮点数值（底层存储值除以 1000f）。
        /// </summary>
        public static float GetAsFloat(this NumericComponent self, int numericType)
        {
            return self.GetAsFloat((NumericType)numericType);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 获取浮点数值（底层存储值除以 1000f）。
        /// </summary>
        public static float GetAsFloat(this NumericComponent self, NumericType numericType)
        {
            return (float)self.GetByKey(numericType) / 1000;
        }

        /// <summary>
        /// 按整型属性 ID 获取 int 数值。
        /// </summary>
        public static int Get(this NumericComponent self, int numericType)
        {
            return self.Get((NumericType)numericType);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 获取 int 数值。
        /// </summary>
        public static int Get(this NumericComponent self, NumericType numericType)
        {
            return (int)self.GetByKey(numericType);
        }

        /// <summary>
        /// 按整型属性 ID 获取 int 数值。
        /// </summary>
        public static int GetAsInt(this NumericComponent self, int numericType)
        {
            return self.GetAsInt((NumericType)numericType);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 获取 int 数值。
        /// </summary>
        public static int GetAsInt(this NumericComponent self, NumericType numericType)
        {
            return (int)self.GetByKey(numericType);
        }

        /// <summary>
        /// 按整型属性 ID 获取 long 数值。
        /// </summary>
        public static long GetAsLong(this NumericComponent self, int numericType)
        {
            return self.GetAsLong((NumericType)numericType);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 获取 long 数值。
        /// </summary>
        public static long GetAsLong(this NumericComponent self, NumericType numericType)
        {
            return self.GetByKey(numericType);
        }

        /// <summary>
        /// 按整型属性 ID 设置浮点数值（乘以 1000 转为 long 存储并触发事件）。
        /// </summary>
        public static void Set(this NumericComponent self, int nt, float value)
        {
            self.Set((NumericType)nt, value);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 设置浮点数值（乘以 1000 转为 long 存储并触发事件）。
        /// </summary>
        public static void Set(this NumericComponent self, NumericType nt, float value)
        {
            self.Insert(nt, (long)(value * 1000));
        }

        /// <summary>
        /// 按整型属性 ID 设置 int 数值并触发事件。
        /// </summary>
        public static void Set(this NumericComponent self, int nt, int value)
        {
            self.Set((NumericType)nt, value);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 设置 int 数值并触发事件。
        /// </summary>
        public static void Set(this NumericComponent self, NumericType nt, int value)
        {
            self.Insert(nt, value);
        }

        /// <summary>
        /// 按整型属性 ID 设置 long 数值并触发事件。
        /// </summary>
        public static void Set(this NumericComponent self, int nt, long value)
        {
            self.Set((NumericType)nt, value);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 设置 long 数值并触发事件。
        /// </summary>
        public static void Set(this NumericComponent self, NumericType nt, long value)
        {
            self.Insert(nt, value);
        }

        /// <summary>
        /// 按整型属性 ID 静默设置数值（只重算关联属性，不派发 <see cref="NumbericChange"/> 事件，常用于实体初始化）。
        /// </summary>
        public static void SetNoEvent(this NumericComponent self, int numericType, long value)
        {
            self.SetNoEvent((NumericType)numericType, value);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 静默设置数值（只重算关联属性，不派发 <see cref="NumbericChange"/> 事件，常用于实体初始化）。
        /// </summary>
        public static void SetNoEvent(this NumericComponent self, NumericType numericType, long value)
        {
            self.Insert(numericType, value, false);
        }

        /// <summary>
        /// 按整型属性 ID 写入数值（处理上限截断、属性联动及事件广播）。
        /// </summary>
        public static void Insert(this NumericComponent self, int numericType, long value, bool isPublicEvent = true)
        {
            self.Insert((NumericType)numericType, value, isPublicEvent);
        }

        /// <summary>
        /// 写入指定 <see cref="NumericType"/> 的数值：
        /// 若为二级属性（>=10000）则重算一级最终属性；若为一级属性则检查最大值限制、计算对其他二级属性的衍生影响，并按需发布 <see cref="NumbericChange"/> 事件。
        /// </summary>
        public static void Insert(this NumericComponent self, NumericType numericType, long value, bool isPublicEvent = true)
        {
            long oldValue = self.GetByKey(numericType);
            if (oldValue == value)
            {
                return;
            }

            self.NumericDic[numericType] = value;

            int numericTypeValue = (int)numericType;
            if (numericTypeValue >= Max)
            {
                self.Update(numericType, isPublicEvent);
                return;
            }

            // 如果有最大值，需要限制最大值
            NumericTypeConfig numericTypeConfig = self.Fiber().GetSingleton<NumericTypeConfigCategory>().Get(numericTypeValue);
            if (numericTypeConfig.MaxNumericType > 0)
            {
                long max = self.GetByKey(numericTypeConfig.MaxNumericType);
                if (value > max)
                {
                    value = max;
                }
            }

            // 影响其它数值
            if (numericTypeConfig.AffectNumeric.Count > 0)
            {
                foreach (var kv in numericTypeConfig.AffectNumeric)
                {
                    // 影响的值必须大于Max，这样才能恢复回去
                    if (kv.Key < Max)
                    {
                        Log.Error($"AffectNumericType is {kv.Key}, numericType is {numericTypeConfig.Id}");
                        continue;
                    }
                    NumericType affectNumericType = (NumericType)kv.Key;
                    long affectOldValue = self.GetByKey(affectNumericType);
                    self.Insert(affectNumericType, affectOldValue + kv.Value * (value - oldValue), isPublicEvent);
                }
            }

            if (isPublicEvent)
            {
                Unit unit = self.GetParent<Unit>();
                if (unit == null)
                {
                    return;
                }
                EventSystem.Instance.Publish(self.Scene(),
                    new NumbericChange() { Unit = unit, New = value, Old = oldValue, NumericType = numericType });
            }
        }

        /// <summary>
        /// 按整型键从字典中读取原始 long 值（不存在时返回 0）。
        /// </summary>
        public static long GetByKey(this NumericComponent self, int key)
        {
            return self.GetByKey((NumericType)key);
        }

        /// <summary>
        /// 按 <see cref="NumericType"/> 键从字典中读取原始 long 值（不存在时返回 0）。
        /// </summary>
        public static long GetByKey(this NumericComponent self, NumericType key)
        {
            long value = 0;
            self.NumericDic.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// 根据二级属性 ID 重算其所属的一级最终属性。
        /// </summary>
        public static void Update(this NumericComponent self, int numericType, bool isPublicEvent)
        {
            self.Update((NumericType)numericType, isPublicEvent);
        }

        /// <summary>
        /// 根据五元组公式 <c>final = (((base + add) * (100 + pct) / 100) + finalAdd) * (100 + finalPct) / 100</c> 重新计算一级最终属性并写入。
        /// </summary>
        public static void Update(this NumericComponent self, NumericType numericType, bool isPublicEvent)
        {
            int numericTypeValue = (int)numericType;
            int final = numericTypeValue / 10;
            int bas = final * 10 + 1;
            int add = final * 10 + 2;
            int pct = final * 10 + 3;
            int finalAdd = final * 10 + 4;
            int finalPct = final * 10 + 5;

            // 一个数值可能会多种情况影响，比如速度,加个buff可能增加速度绝对值100，也有些buff增加10%速度，所以一个值可以由5个值进行控制其最终结果
            // final = (((base + add) * (100 + pct) / 100) + finalAdd) * (100 + finalPct) / 100;
            int pctValue = self.GetAsInt(pct);
            if (pctValue < -100)
            {
                pctValue = -100;
            }
            int finalPctValue = self.GetAsInt(finalPct);
            if (finalPctValue < -100)
            {
                finalPctValue = -100;
            }
            long result = (long)(((self.GetByKey(bas) + self.GetByKey(add)) * (100 + pctValue) / 100f + self.GetByKey(finalAdd)) * (100 + finalPctValue) / 100f);
            self.Insert(final, result, isPublicEvent);
        }
    }
}
