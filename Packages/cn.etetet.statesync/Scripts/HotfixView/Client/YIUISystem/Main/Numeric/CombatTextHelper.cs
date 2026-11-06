using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 战斗飘字静态辅助类，根据单位生命值（HP）变化量自动判定伤害、暴击或治疗并触发飘字表现
    /// </summary>
    public static class CombatTextHelper
    {
        /// <summary>
        /// 触发飘字显示的最小生命值变化阈值
        /// </summary>
        private const long MinDisplayValue = 1;

        /// <summary>
        /// 判定为暴击飘字的单次伤害占最大生命值百分比阈值（15%）
        /// </summary>
        private const int CriticalDamageThresholdPercent = 15;

        /// <summary>
        /// 根据单位新旧 HP 差值在单位头顶上方显示伤害（红色/暴击金黄色）或治疗（绿色）飘字
        /// </summary>
        /// <param name="scene">所属客户端场景</param>
        /// <param name="unit">发生生命值变化的单位</param>
        /// <param name="oldHp">变化前生命值</param>
        /// <param name="newHp">变化后生命值</param>
        public static void ShowHpChange(Scene scene, Unit unit, long oldHp, long newHp)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            long delta = newHp - oldHp;
            if (delta == 0)
            {
                return;
            }

            CombatTextComponent combatTextComponent = GetOrCreateCombatTextComponent(scene);
            if (combatTextComponent == null)
            {
                return;
            }

            var unitPos = unit.Position;
            Vector3 worldPos = new Vector3(unitPos.x, unitPos.y + 2.0f, unitPos.z);

            if (delta < 0)
            {
                long damageValue = -delta;
                if (damageValue < MinDisplayValue)
                {
                    return;
                }

                bool isCritical = IsCriticalDamage(unit, damageValue);
                combatTextComponent.Show(worldPos, FormatValue(damageValue),
                    isCritical ? new Color(1.0f, 0.76f, 0.2f, 1.0f) : new Color(1.0f, 0.2f, 0.2f, 1.0f),
                    isCritical ? 52 : 44,
                    isCritical);
                return;
            }

            long healValue = delta;

            combatTextComponent.Show(worldPos, $"+{FormatValue(healValue)}", new Color(0.2f, 1.0f, 0.35f, 1.0f), 42, false);
        }

        private static CombatTextComponent GetOrCreateCombatTextComponent(Scene scene)
        {
            CombatTextComponent component = scene.GetComponent<CombatTextComponent>();
            if (component != null)
            {
                return component;
            }

            return scene.AddComponent<CombatTextComponent>();
        }

        private static string FormatValue(long value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:0.#}M";
            }

            if (value >= 1000)
            {
                return $"{value / 1000f:0.#}K";
            }

            return value.ToString();
        }

        private static bool IsCriticalDamage(Unit unit, long damageValue)
        {
            if (damageValue <= 0)
            {
                return false;
            }

            NumericComponent numericComponent = unit.NumericComponent;
            if (numericComponent == null)
            {
                return false;
            }

            long maxHp = numericComponent.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                return false;
            }

            return damageValue * 100 >= maxHp * CriticalDamageThresholdPercent;
        }
    }
}
