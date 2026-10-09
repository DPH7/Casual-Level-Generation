using System;
using System.Collections.Generic;
using System.Linq;

public class ForwardSolver
{
    private const int MaxHolderSize = 4; // 你的手牌区限制：4个槽位

    /// <summary>
    /// 模拟玩家消除，验证在 4 个卡槽限制下是否可行[cite: 1]
    /// </summary>
    public bool ValidateSolvable(List<Slot> allSlots)
    {
        List<TileType> holder = new List<TileType>(); // 4个手牌槽[cite: 1]
        HashSet<int> eliminatedSlots = new HashSet<int>();
        
        while (eliminatedSlots.Count < allSlots.Count)
        {
            // 获取当前玩家正向可见/可点击的卡牌
            var playableSlots = allSlots
                .Where(s => !eliminatedSlots.Contains(s.Id) && s.BlockingSlots.All(b => eliminatedSlots.Contains(b)))
                .ToList();

            bool moved = false;

            // 优先规则 1：如果手牌里已有某张牌，且场上有对应的可消牌，立即点击匹配消除
            foreach (var slot in playableSlots)
            {
                if (holder.Contains(slot.AssignedType))
                {
                    holder.Remove(slot.AssignedType); // 凑齐 2 张消除[cite: 1]
                    eliminatedSlots.Add(slot.Id);
                    moved = true;
                    break;
                }
            }

            // 优先规则 2：若无法直接消除，且手牌区未满 ( < 4)，放一张新牌进去[cite: 1]
            if (!moved && holder.Count < MaxHolderSize)
            {
                // 贪心策略：选择能释放最多下层牌的槽位
                Slot bestSlot = playableSlots
                    .OrderByDescending(s => s.Dependents.Count(d => !eliminatedSlots.Contains(d)))
                    .FirstOrDefault();

                if (bestSlot != null)
                {
                    holder.Add(bestSlot.AssignedType);
                    eliminatedSlots.Add(bestSlot.Id);
                    moved = true;
                }
            }

            // 如果无法再走任何一步且手牌区已满，说明此牌型在4槽位下难度过高/死局[cite: 1]
            if (!moved) return false; 
        }

        return true; // 成功通关
    }
}