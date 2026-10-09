using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random; // 如果是 Unity 项目，建议使用这个

public enum Difficulty
{
    Easy,   // 优先成对放置在邻近/同层槽位
    Hard    // 故意制造跨层、深层交叉依赖
}
public class LevelGenerator
{
    private System.Random random = new System.Random();
    private Slot lastUnlockedSlot = null; // 记录上一轮逆向放牌后最新被释放/解锁的槽位
    /// <summary>
    /// 生成关卡牌型分布
    /// </summary>
    /// <param name="allSlots">所有槽位节点列表（包含建立好的遮挡关系）</param>
    /// <param name="totalPairs">总对数（例如 20 对 = 40 张牌）</param>
    /// <param name="difficulty">难度</param>
    public List<SlotPair> GenerateLevel(List<Slot> allSlots)
    {
        // 1. 初始化逆向填充的可用池（所有槽位初始未分配）
        List<Slot> unassignedSlots = new List<Slot>(allSlots);
        List<Slot> exposedList = new List<Slot>();
        List<SlotPair> pairSlotList = new List<SlotPair>();
        // 记录上一轮分配后，由于拓扑依赖被解除而“新暴露”出来的槽位队列（即正向消除上一对后会露出的卡牌 C）
        Queue<Slot> chainUnlockedQueue = new Queue<Slot>();

        int poolIndex = 0;

        while (unassignedSlots.Count >= 2)
        {
            Slot slotA = null;
            Slot slotB = null;
            exposedList = GetExposedSlots(unassignedSlots);
            
            // ------------------ 步骤 1：挑选 SlotA ------------------
            // 优先从上一步解锁出来的槽位中挑选，形成无缝衔接的消除路线
            if (chainUnlockedQueue.Count > 0 && exposedList.Contains(chainUnlockedQueue.Peek()))
            {
                slotA = chainUnlockedQueue.Dequeue();
            }
            else
            {
                // 若没有连续链条
                slotA = exposedList[Random.Range(0, exposedList.Count)];
            }

            /// ------------------ 步骤 2：挑选 SlotB (控制匹配模式) ------------------
            List<Slot> candidates = exposedList.Where(s => s != slotA).ToList();

            if (candidates.Count == 0)
            {
                // 没有可配对的候选 SlotB，舍弃当前关卡
                Debug.LogWarning("[关卡生成失败] 没有可用的 SlotB 候选，舍弃本关卡！");
                break;
            }

            // 打乱 candidates 顺序，保证配对的随机性
            List<Slot> shuffledCandidates = candidates
                .OrderByDescending(s => s.Layer)       // 1. 优先按 Layer 降序（较大的排在前面）
                .ThenBy(_ => UnityEngine.Random.value) // 2. 同 Layer 内的候选者随机乱序
                .ToList();

            bool foundValidSlotB = false;

            foreach (var candidateB in shuffledCandidates)
            {
                // 计算移除 slotA 和 candidateB 后，最新解封的卡牌列表（按层级降序）
                List<Slot> newlyUnlocked = GetNewlyUnlockedSlotsInReverse(unassignedSlots, slotA, candidateB)
                    .OrderByDescending(s => s.Layer)
                    .ToList();

                // 找到第一个不在队列中的解锁节点
                Slot nextChainSlot = newlyUnlocked.FirstOrDefault(s => !chainUnlockedQueue.Contains(s));

                // 校验条件：
                // 1. 成功算出了新的可解封节点 (nextChainSlot != null)
                // 2. 特殊情况：如果是最后一对牌 (剩余 total <= 2)，后面本来就没有牌了，允许 newlyUnlocked 为 0
                bool isLastPair = unassignedSlots.Count <= 2;

                if (nextChainSlot != null || isLastPair)
                {
                    slotB = candidateB;
                    foundValidSlotB = true;

                    if (nextChainSlot != null)
                    {
                        chainUnlockedQueue.Enqueue(nextChainSlot);
                    }

                    break; // 成功找到合规的 slotB，跳出循环
                }
            }

            // 如果遍历完所有 candidates 依然没有一个能产生新解锁节点，舍弃这一关
            if (!foundValidSlotB)
            {
                Debug.LogWarning("[关卡生成失败] 所有 candidateB 都无法产生新的解锁节点 (陷入死锁)，舍弃当前关卡！");
                break;
            }
            
            pairSlotList.Add(new SlotPair(slotA, slotB));
            
            // ------------------ 步骤 4：核心清理！从候选池彻底移除已配对槽位 ------------------
            unassignedSlots.Remove(slotA);
            unassignedSlots.Remove(slotB);
        }

        return pairSlotList;
    }
    
    public static List<Slot> GetExposedSlots(List<Slot> unassignedSlots)
    {
        List<Slot> exposedList = new List<Slot>();

        // 建立 HashHashSet 提高查询效率（仅在 unassignedSlots 里的牌算作“未被消除”）
        HashSet<int> activeSlotIds = new HashSet<int>(unassignedSlots.Select(s => s.Id));

        foreach (var slot in unassignedSlots)
        {
            // 1. 上方检查：TopBlockingSlots 里必须没有任何未消除的牌（如果压在上面的牌全被消了，Sum/Any 就会为 false）
            bool hasTopBlock = slot.BlockingSlots.Any(top => activeSlotIds.Contains(top));
            if (hasTopBlock) continue; // 上方有遮挡，直接排除！深层卡牌在这里就被过滤掉了

            // 2. 左右侧检查：左侧未消 OR 右侧未消
            bool hasLeftBlock = slot.LeftBlockingSlots.Any(left => activeSlotIds.Contains(left));
            bool hasRightBlock = slot.RightBlockingSlots.Any(right => activeSlotIds.Contains(right));

            // 规则：上方无遮挡 且 (左边没牌 OR 右边没牌)
            if (!hasLeftBlock || !hasRightBlock)
            {
                exposedList.Add(slot);
            }
        }

        return exposedList;
    }

    /// <summary>
    /// 模拟逆向推导：计算放了 slotA 和 slotB 之后，使得哪些原本被阻塞的 Slot 变成了新的“自由节点”
    /// </summary>
    private List<Slot> GetNewlyUnlockedSlotsInReverse(
        List<Slot> unassignedSlots, 
        Slot slotA, 
        Slot slotB)
    {
        // 1. 计算消掉 A 和 B 之前（包含 A, B 时）场上所有【可点击】的卡牌
        List<Slot> exposedBefore = GetExposedSlots(unassignedSlots);

        // 2. 模拟消掉 A 和 B（即计算不含 A, B 的剩余集合）
        List<Slot> remainingSlots = unassignedSlots.Where(s => s != slotA && s != slotB).ToList();

        // 3. 计算消掉 A 和 B 之后，场上所有【可点击】的卡牌
        List<Slot> exposedAfter = GetExposedSlots(remainingSlots);

        // 4. 差集：在『消掉后』变成了可点击、但『消掉前』不可点击的卡牌，才是真正新增的解封线索！
        HashSet<int> beforeIds = new HashSet<int>(exposedBefore.Select(s => s.Id));
    
        List<Slot> newlyUnlocked = exposedAfter
            .Where(s => !beforeIds.Contains(s.Id))
            .ToList();

        return newlyUnlocked;
    }
    
    /// <summary>
    /// 为已经构建好拓扑结构的 Pair 序列进行“正向时空染色”
    /// </summary>
    public bool ColorLevel(List<SlotPair> pairSequence, List<Slot> allSlots, List<TileType> tilePool)
    {
        // 1. 复制一份可用花色池
        List<TileType> remainingPool = new List<TileType>(tilePool);
        System.Random random = new System.Random();

        // 2. 维护正向游戏中当前“存活/未被消除”的 Slot 集合
        HashSet<Slot> activeSlots = new HashSet<Slot>(allSlots);

        // 3. 逐对进行正向时空染色
        for (int step = 0; step < pairSequence.Count; step++)
        {
            SlotPair currentPair = pairSequence[step];

            // 尝试为当前 Pair 挑选一个合规的花色
            TileType selectedType = PickValidTypeForStep(currentPair, pairSequence, activeSlots, allSlots, remainingPool, random);

            if (selectedType == TileType.None)
            {
                Debug.LogError($"[染色失败] 在第 {step} 步无法找到合规花色，可能花色池种类不足！");
                return false;
            }

            // 确定赋值
            currentPair.SlotA.AssignedType = selectedType;
            currentPair.SlotB.AssignedType = selectedType;
            currentPair.AssignedType = selectedType;

            // 消耗花色池（扣除 2 张）
            remainingPool.Remove(selectedType);
            remainingPool.Remove(selectedType);

            // 模拟玩家消掉了这一对：将其从存活集合中移除
            activeSlots.Remove(currentPair.SlotA);
            activeSlots.Remove(currentPair.SlotB);
        }

        return true;
    }

    /// <summary>
    /// 模拟当前步骤的盘面，挑选不触发“3张相同/过载”的花色
    /// </summary>
    private TileType PickValidTypeForStep(
        SlotPair currentPair,
        List<SlotPair> pairSequence,
        HashSet<Slot> activeSlots,
        List<Slot> allSlots,
        List<TileType> candidatePool,
        System.Random random)
    {
        // 随机打乱候选花色，增加关卡多样性
        var distinctCandidates = candidatePool.Distinct().OrderBy(_ => random.Next()).ToList();

        foreach (var candidate in distinctCandidates)
        {
            // 预赋花色
            currentPair.SlotA.AssignedType = candidate;
            currentPair.SlotB.AssignedType = candidate;

            // 🔍 校验：在当前存活的盘面上，此赋值是否会导致任何“暴露花色数量为奇数（如3张）”
            if (CheckForwardExposureValidity(activeSlots))
            {
                return candidate; // 校验通过！
            }

            // 校验失败，撤销预赋
            currentPair.SlotA.AssignedType = TileType.None;
            currentPair.SlotB.AssignedType = TileType.None;
        }

        // 若受到极端拓扑限制无完美解，退而求其次选择第一个
        return distinctCandidates.FirstOrDefault();
    }

    /// <summary>
    /// 正向盘面暴露合法性检测
    /// </summary>
    private bool CheckForwardExposureValidity(HashSet<Slot> activeSlots)
    {
        // 1. 获取当前活着的 Slot 中，按几何规则“处于暴露状态”的卡牌列表
        List<Slot> currentlyExposed = GetExposedSlots(activeSlots.ToList());

        // 2. 过滤出其中【已经被赋了花色】的 Slot
        var typedExposedSlots = currentlyExposed.Where(s => s.AssignedType != TileType.None).ToList();

        // 3. 按花色分组统计
        var groups = typedExposedSlots.GroupBy(s => s.AssignedType);

        int matchablePairCount = 0;

        foreach (var group in groups)
        {
            int count = group.Count();

            // 🚫 核心校验 1：暴露在外且已赋花色的同种卡牌数量绝不能为奇数（排除 1 张、3 张、5 张）
            // 只要不是奇数，就绝对不会出现“3 张在场让玩家陷入选错陷阱”的情况！
            if (count % 2 != 0)
            {
                return false;
            }

            matchablePairCount += count / 2;
        }

        // 🚫 核心校验 2：当前场上暴露可消的对数不能超过 2 对
        if (matchablePairCount > 2)
        {
            return false;
        }

        return true;
    }
}