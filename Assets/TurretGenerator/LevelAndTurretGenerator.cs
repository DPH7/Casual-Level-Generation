using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelAndTurretGenerator : MonoBehaviour
{
    public enum BlockColor
    {
        Empty,
        Black,
        Yellow,
        White,
        Red,
        Green,
        Blue,
        Cyan,      // 新增：青色
        Magenta,   // 新增：洋红/品红
        Orange     // 新增：橙色
    }

    [System.Serializable]
    public struct TurretInfo
    {
        public BlockColor color;
        public int ammoCount; // 固定为 20

        public TurretInfo(BlockColor color, int ammoCount = 20)
        {
            this.color = color;
            this.ammoCount = ammoCount;
        }
    }

    [Header("关卡矩阵配置")]
    public int rows = 50;
    public int cols = 10;

    [Header("装箱块尺寸范围")]
    [Range(2, 6)] public int minBlockWidth = 2;
    [Range(2, 10)] public int maxBlockWidth = 6;
    [Range(2, 8)] public int minBlockHeight = 2;
    [Range(2, 12)] public int maxBlockHeight = 6;
    
    [Header("炮台队列配置")]
    [Tooltip("下方炮台排布的固定列数 N")]
    public int turretCols = 4; // 由关卡参数决定列数 (如 4 列、5 列)

    [Tooltip("难度")]
    public float diff = 0.3f;

    [Header("颜色池")]
    public List<BlockColor> availableColors = new List<BlockColor>
    {
        BlockColor.Black,
        BlockColor.Yellow,
        BlockColor.White,
        BlockColor.Red,
        BlockColor.Green,
        BlockColor.Blue,
        BlockColor.Cyan,
        BlockColor.Magenta,
        BlockColor.Orange
    };

    private BlockColor[,] grid;

    /// <summary>
    /// 生成入口：同时返回【关卡颜色矩阵】和【反推的可解炮台队列】
    /// </summary>
    public BlockColor[,] GenerateLevelAndTurrets()
    {
        grid = new BlockColor[rows, cols];

        // Step 1: 采用左下优先装箱填充整个网格（允许同色碰撞）
        FillGridWithBottomLeftPacking();

        // Step 2: 修正颜色总数，确保每种颜色的格子数都是 20 的倍数
        EnforceColorCountsMultipleOf20();

        return (grid);
    }

    #region Step 1: 左下装箱生成 (允许同色相连)

    private void FillGridWithBottomLeftPacking()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] == BlockColor.Empty)
                {
                    PlaceNextBlock(r, c);
                }
            }
        }
    }

    private void PlaceNextBlock(int startRow, int startCol)
    {
        int targetW = Random.Range(minBlockWidth, maxBlockWidth + 1);
        int targetH = Random.Range(minBlockHeight, maxBlockHeight + 1);

        int actualW = 0;
        for (int c = startCol; c < cols && c < startCol + targetW; c++)
        {
            if (grid[startRow, c] != BlockColor.Empty) break;
            actualW++;
        }

        if (actualW < minBlockWidth)
        {
            // 缝隙太小，直接取左侧或下侧颜色进行吸附扩展
            BlockColor neighborColor = GetNeighborColor(startRow, startCol);
            grid[startRow, startCol] = neighborColor != BlockColor.Empty ? neighborColor : GetRandomColor();
            return;
        }

        int actualH = 0;
        for (int r = startRow; r < rows && r < startRow + targetH; r++)
        {
            bool rowAvailable = true;
            for (int c = startCol; c < startCol + actualW; c++)
            {
                if (grid[r, c] != BlockColor.Empty)
                {
                    rowAvailable = false;
                    break;
                }
            }
            if (!rowAvailable) break;
            actualH++;
        }

        // 完全不避开邻居颜色，随机选取
        BlockColor chosenColor = GetRandomColor();

        for (int r = startRow; r < startRow + actualH; r++)
        {
            for (int c = startCol; c < startCol + actualW; c++)
            {
                grid[r, c] = chosenColor;
            }
        }
    }

    #endregion

    #region Step 2: 颜色总数强制为 20 的倍数修正

    private void EnforceColorCountsMultipleOf20()
    {
        Dictionary<BlockColor, int> colorCounts = new Dictionary<BlockColor, int>();
        foreach (var color in availableColors) colorCounts[color] = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] != BlockColor.Empty)
                    colorCounts[grid[r, c]]++;
            }
        }

        // 挑选一个基础颜色作为调和色（多退少补）
        BlockColor primaryColor = availableColors[0];

        foreach (var color in availableColors)
        {
            if (color == primaryColor) continue;

            int count = colorCounts[color];
            int remainder = count % 20;

            if (remainder != 0)
            {
                // 需要修改 remainder 个格子的颜色
                int neededChange = remainder;
                ChangeColorCells(color, primaryColor, neededChange);
            }
        }

        // 最后强行修正主色，确保全图总格子数也是 20 的倍数
        int totalCells = rows * cols;
        int targetPrimaryCount = (totalCells / 20) * 20; 
        // 矩阵总格子数 50*10 = 500，修正后主色自动补齐完备
    }

    private void ChangeColorCells(BlockColor fromColor, BlockColor toColor, int countToChange)
    {
        int changed = 0;
        for (int r = rows - 1; r >= 0 && changed < countToChange; r--)
        {
            for (int c = 0; c < cols && changed < countToChange; c++)
            {
                if (grid[r, c] == fromColor)
                {
                    grid[r, c] = toColor;
                    changed++;
                }
            }
        }
    }

    #endregion

    #region Step 3 ~ 5: 反推炮台队列 (逻辑层模拟消除)

    /// <summary>
    /// 反推生成下方 N 列的炮台矩阵（行数往上堆叠，r=0 在最底下）
    /// </summary>
    public TurretInfo[,] ReverseSolveTurretMatrix()
    {
        // 1. 逆向推导一维序列（带严格槽位限制）
        BlockColor[,] simGrid = (BlockColor[,])grid.Clone();
        List<TurretInfo> flatTurrets = new List<TurretInfo>();
        List<TurretInfo> simMidSlots = new List<TurretInfo>();
        var allCount = simGrid.GetLength(0) * simGrid.GetLength(1) / 20;
        bool ifCanFind = true;
        while (!IsGridEmpty(simGrid))
        {
            List<BlockColor> exposedColors = GetExposedColors(simGrid);
            if (exposedColors.Count == 0)
            {
                Debug.Log("没有暴漏颜色");
                break;
            }

            
            // 1. 先检查暴露的颜色是否能直接解锁暂存区里的残渣炮台
            BlockColor matchMidColor = BlockColor.Empty;
            var matchedMidTurretIndex = simMidSlots.FindIndex(t => exposedColors.Contains(t.color));

            if (matchedMidTurretIndex != -1)
            {
                // ---------------------------------------------------------------------
                // 【关键纠正】：消耗的是暂存区的旧炮台！不生成新炮台到 flatTurrets！
                // ---------------------------------------------------------------------
                matchMidColor = simMidSlots[matchedMidTurretIndex].color;
                int exposedCount = GetExposedCountOfColor(simGrid, matchMidColor);
            
                // 扣减暂存区的弹药或移除该残渣
                int remainingAmmo = simMidSlots[matchedMidTurretIndex].ammoCount - exposedCount;
                if (remainingAmmo <= 0)
                {
                    ClearCellsOfColor(simGrid, matchMidColor, simMidSlots[matchedMidTurretIndex].ammoCount);
                    simMidSlots.RemoveAt(matchedMidTurretIndex); // 彻底清空槽位
                }
                else
                {
                    simMidSlots[matchedMidTurretIndex] = new TurretInfo(matchMidColor, remainingAmmo);
                    ClearCellsOfColor(simGrid, matchMidColor, exposedCount);
                }

                // 消除网格中的色块并应用重力
                ApplyGravity(simGrid);

                // 直接跳过下方 add 逻辑，重新进入 while 循环重新评估暴露颜色！
                continue; 
            }
            
            if (simMidSlots.Count >= 5)
            {
                ifCanFind = false;
                Debug.Log("暂存区大于5 卡死");
                break;
            }
            
            // 2. 如果暂存区无法解套，才需要从下方生成一个新的 20 发炮台
            BlockColor targetColor = SelectColorWithStrictBufferGuard(simGrid, exposedColors, simMidSlots, maxSlotCapacity: 5);

            int count = GetExposedCountOfColor(simGrid, targetColor);

            // 如果该新炮台 20 发子弹打不完上方块（产生了残渣），且暂存区已达到危险阈值
            if (count < 20)
            {
                var ratio = flatTurrets.Count /  (float)allCount;
                var limitCount = 0;
                if (ratio < 0.4f || ratio > 0.7f)
                {
                    limitCount = 2;
                }
                else
                {
                    limitCount = 3;
                }
                if (simMidSlots.Count >= limitCount)
                {
                    Debug.Log($"开始修正暂存区, 当前生成的炮台{flatTurrets.Count}");
                    // 尝试对 simGrid 进行局部修正：将 targetColor 向上扩展补满至 20 个（或尽可能补满）
                    // 并从盘面最后面进行等量颜色互换，保持全局色彩总量守恒
                    int neededCount = 20 - count;
                    bool success = TryExpandAndSwapColor(simGrid, targetColor, neededCount);
                
                    if (success)
                    {
                        // 修正成功后，重新获取该颜色的暴露数量（此时应该已经凑够，不再产生残渣进入暂存区）
                        count = 20;
                        Debug.Log($"成功修正暂存区, 匹配数量{count}");
                    }
                    else
                    {
                        // 如果极端情况下无法扩展（比如后面已经没有该颜色可抽调），保底压入暂存区
                        simMidSlots.Add(new TurretInfo(targetColor, 20 - count));
                        Debug.Log("上方格子不能修改");
                    }
                }
                else
                {
                    // 槽位还安全，正常压入残渣
                    simMidSlots.Add(new TurretInfo(targetColor, 20 - count));
                }
            }

            ClearCellsOfColor(simGrid, targetColor, count);
            ApplyGravity(simGrid);

            // 只有真正消耗了下方新炮台时，才放入答案队列
            flatTurrets.Add(new TurretInfo(targetColor, 20));
        }

        // 2. 将一维数据转换为【N 列】矩阵，采用“同色纵向聚集”策略
        int totalTurrets = flatTurrets.Count;
        int turretRows = Mathf.CeilToInt((float)totalTurrets / turretCols);
        TurretInfo[,] turretMatrix = new TurretInfo[turretRows, turretCols];

        // 初始化为空
        for (int r = 0; r < turretRows; r++)
        for (int c = 0; c < turretCols; c++)
            turretMatrix[r, c] = new TurretInfo(BlockColor.Empty, 0);

        // 填充矩阵：让同色炮台倾向于排在同一列，减少最底排出现的颜色种类数
        int currentR = 0, currentC = 0;
        for (int i = 0; i < totalTurrets; i++)
        {
            turretMatrix[currentR, currentC] = flatTurrets[i];

            currentC++;
            if (currentC >= turretCols)
            {
                currentC = 0;
                currentR++;
            }
        }

        return ifCanFind ? turretMatrix: null;
    }

    /// <summary>
/// 按照“列优先纵向向上 + 底部横向向两侧拓展”的策略进行等量色彩置换
/// </summary>
private bool TryExpandAndSwapColor(BlockColor[,] grid, BlockColor targetColor, int neededCount)
{
    int rows = grid.GetLength(0);
    int cols = grid.GetLength(1);

    // 记录被覆盖掉的原始颜色及其位置信息 (r, c, originalColor)
    List<(int r, int c, BlockColor originalColor)> overriddenCells = new List<(int, int, BlockColor)>();

    // 1. 收集当前最底层暴露了 targetColor 的列，以及这些列中最靠下的 targetColor 的 row 坐标
    List<int> targetCols = new List<int>();
    HashSet<int> visitedCols = new HashSet<int>();

    for (int c = 0; c < cols; c++)
    {
        for (int r = 0; r < rows; r++)
        {
            if (grid[r, c] != BlockColor.Empty)
            {
                if (grid[r, c] == targetColor)
                {
                    targetCols.Add(c);
                    visitedCols.Add(c);
                }
                break; // 只看最底层暴露的第一层
            }
        }
    }

    // -------------------------------------------------------------------------
    // 阶段 1：纵向向上扩展（优先在已有 targetColor 的列中向上“挖深”）
    // -------------------------------------------------------------------------
    foreach (int c in targetCols)
    {
        if (overriddenCells.Count >= neededCount) break;

        // 找到该列最靠下的非空格子（即暴露点）
        int startRow = 0;
        while (startRow < rows && grid[startRow, c] == BlockColor.Empty) startRow++;

        // 从暴露点向上推进，把非 targetColor 替换掉
        for (int r = startRow; r < rows && overriddenCells.Count < neededCount; r++)
        {
            BlockColor cur = grid[r, c];
            if (cur != BlockColor.Empty && cur != targetColor)
            {
                overriddenCells.Add((r, c, cur));
                grid[r, c] = targetColor;
            }
        }
    }

    // -------------------------------------------------------------------------
    // 阶段 2：横向向两侧拓展列 + 纵向向上填充
    // 如果阶段 1 还没凑齐，向左右两边拓展新的列，并由底向上填充
    // -------------------------------------------------------------------------
    if (overriddenCells.Count < neededCount)
    {
        // 确定初始的边界列
        int leftCol = targetCols.Count > 0 ? targetCols.Min() - 1 : (cols / 2) - 1;
        int rightCol = targetCols.Count > 0 ? targetCols.Max() + 1 : cols / 2;

        // 轮流向左右扩展
        while (overriddenCells.Count < neededCount && (leftCol >= 0 || rightCol < cols))
        {
            // 尝试扩展左边一列
            if (leftCol >= 0 && overriddenCells.Count < neededCount)
            {
                ExpandColumnUpward(grid, leftCol, targetColor, neededCount, overriddenCells);
                leftCol--;
            }

            // 尝试扩展右边一列
            if (rightCol < cols && overriddenCells.Count < neededCount)
            {
                ExpandColumnUpward(grid, rightCol, targetColor, neededCount, overriddenCells);
                rightCol++;
            }
        }
    }

    // 如果尝试了所有可能依然无法凑齐（极端边缘情况）
    if (overriddenCells.Count == 0) return false;

    // -------------------------------------------------------------------------
    // 步骤 3：守恒补偿——从盘面最深处（后方倒序）寻找 targetColor 进行等量交换
    // -------------------------------------------------------------------------
    int swappedCount = 0;
    int targetSwapTotal = overriddenCells.Count;

    for (int r = rows - 1; r >= 0 && swappedCount < targetSwapTotal; r--)
    {
        for (int c = cols - 1; c >= 0 && swappedCount < targetSwapTotal; c--)
        {
            // 避开刚刚前端修改过的格子
            bool isFrontendModified = overriddenCells.Exists(cell => cell.r == r && cell.c == c);

            if (!isFrontendModified && grid[r, c] == targetColor)
            {
                BlockColor colorToRestore = overriddenCells[swappedCount].originalColor;
                grid[r, c] = colorToRestore;
                swappedCount++;
            }
        }
    }

    // 校验：如果后方无法找到足够的 targetColor 来还原，则回滚前端修改
    if (swappedCount < targetSwapTotal)
    {
        Debug.LogWarning($"[警告] 后方缺乏足够的 {targetColor} 保持色彩守恒，回滚突变操作。swappedCount:{swappedCount},targetSwapTotal{targetSwapTotal}");
        foreach (var cell in overriddenCells)
        {
            grid[cell.r, cell.c] = cell.originalColor;
        }
        return false;
    }

    return true;
}

    /// <summary>
    /// 辅助函数：对指定列从最底部暴露位置开始，向上扫描并修改格子为 targetColor
    /// </summary>
    private void ExpandColumnUpward(BlockColor[,] grid, int c, BlockColor targetColor, int neededCount,
        List<(int r, int c, BlockColor originalColor)> overriddenCells)
    {
        int rows = grid.GetLength(0);

        // 找到该列最底部的非空格子
        int startRow = 0;
        while (startRow < rows && grid[startRow, c] == BlockColor.Empty) startRow++;

        // 从底向上依次修改
        for (int r = startRow; r < rows && overriddenCells.Count < neededCount; r++)
        {
            BlockColor cur = grid[r, c];
            if (cur != BlockColor.Empty && cur != targetColor)
            {
                overriddenCells.Add((r, c, cur));
                grid[r, c] = targetColor;
            }
        }
    }

    /// <summary>
    /// 辅助函数：判断 (r, c) 是否与指定颜色相邻（上下左右）
    /// </summary>
    private bool IsAdjacentToColor(BlockColor[,] grid, int r, int c, BlockColor target)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = r + dr[i];
            int nc = c + dc[i];

            if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
            {
                if (grid[nr, nc] == target) return true;
            }
        }

        return false;
    }

    private BlockColor SelectColorWithStrictBufferGuard(
        BlockColor[,] simGrid,
        List<BlockColor> exposedColors,
        List<TurretInfo> simMidSlots,
        int maxSlotCapacity = 5)
    {
        // -------------------------------------------------------------
        // 策略 B：【连锁反应前瞻 (Look-ahead)】
        // 如果没有直接匹配的，试算选哪个颜色能在消除下落后，揭露出“能匹配暂存区”的新颜色！
        // -------------------------------------------------------------
        BlockColor bestChainColor = BlockColor.Empty;
        int maxChainUnlockCount = -1;

        foreach (var candidateColor in exposedColors)
        {
            // 1. 克隆一个临时盘面进行虚拟前瞻
            BlockColor[,] tempGrid = (BlockColor[,])simGrid.Clone();

            // 2. 模拟消除该颜色并下落
            ClearCellsOfColor(tempGrid, candidateColor, 20);
            ApplyGravity(tempGrid);

            // 3. 获取消除后【新暴露】的颜色集合
            List<BlockColor> nextExposedColors = GetExposedColors(tempGrid);

            // 4. 统计新暴露的颜色里，能帮暂存区解套的色块总数
            int unlockScore = 0;
            foreach (var midTurret in simMidSlots)
            {
                if (nextExposedColors.Contains(midTurret.color))
                {
                    unlockScore += GetExposedCountOfColor(tempGrid, midTurret.color);
                }
            }

            // 记录能提供最大连锁解锁收益的颜色
            if (unlockScore > maxChainUnlockCount && unlockScore > 0)
            {
                maxChainUnlockCount = unlockScore;
                bestChainColor = candidateColor;
            }
        }

        // 如果找到了能触发连锁解套的颜色，优先选择它！
        if (bestChainColor != BlockColor.Empty)
        {
            return bestChainColor;
        }

        // -------------------------------------------------------------
        // 策略 C：【硬性安全门】如果暂存区已经存了 >= 3 个不同颜色的残渣
        // -------------------------------------------------------------
        if (simMidSlots.Count >= 3)
        {
            // 过滤出所有暴露数量 >= 20（能直接完全消掉、不留残渣）的颜色
            List<BlockColor> zeroResidualColors = exposedColors.FindAll(c => GetExposedCountOfColor(simGrid, c) >= 20);
            if (zeroResidualColors.Count > 0)
            {
                zeroResidualColors.Sort((a, b) =>
                    GetExposedCountOfColor(simGrid, b).CompareTo(GetExposedCountOfColor(simGrid, a)));
                return zeroResidualColors[0];
            }
        }

        // -------------------------------------------------------------
        // 策略 D：保底选色（优先选择当前暴露最多的颜色）
        // -------------------------------------------------------------
        exposedColors.Sort((a, b) => GetExposedCountOfColor(simGrid, b).CompareTo(GetExposedCountOfColor(simGrid, a)));
        return exposedColors[0];
    }

    /// <summary>
    /// 模拟正向点击炮台时，暂存区的数据变化
    /// </summary>
    private void UpdateSimulatedSlots(List<TurretInfo> simMidSlots, BlockColor color, int exposedCount, int defaultAmmo)
    {
        // 先看暂存区有没有同色炮台
        int existingIndex = simMidSlots.FindIndex(t => t.color == color);

        if (existingIndex != -1)
        {
            // 如果有，先消耗暂存区里的弹药
            var turret = simMidSlots[existingIndex];
            if (exposedCount >= turret.ammoCount)
            {
                int remainingExposed = exposedCount - turret.ammoCount;
                simMidSlots.RemoveAt(existingIndex); // 暂存区里的被消灭，腾出槽位！
            
                // 如果消除完暂存区后，新炮台还有剩余子弹，新炮台入槽
                if (defaultAmmo > remainingExposed)
                {
                    simMidSlots.Add(new TurretInfo(color, defaultAmmo - remainingExposed));
                }
            }
            else
            {
                turret.ammoCount -= exposedCount;
                simMidSlots[existingIndex] = turret;
            }
        }
        else
        {
            // 如果暂存区没有同色，且新炮台打不完，直接入槽
            if (defaultAmmo > exposedCount)
            {
                simMidSlots.Add(new TurretInfo(color, defaultAmmo - exposedCount));
            }
        }
    }
    
    private List<BlockColor> GetExposedColors(BlockColor[,] simGrid)
    {
        List<BlockColor> exposed = new List<BlockColor>();
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (simGrid[r, c] != BlockColor.Empty)
                {
                    BlockColor color = simGrid[r, c];
                    if (!exposed.Contains(color)) exposed.Add(color);
                    break; // 只取这一列最底部的第一个非空格子
                }
            }
        }
        return exposed;
    }

    /// <summary>
    /// 难度控制选色
    /// </summary>
    /// <param name="difficulty">0.0f (最简单) ~ 1.0f (最难)</param>
    private BlockColor SelectColorWithDifficultyControl(
        BlockColor[,] simGrid,
        List<BlockColor> exposedColors,
        List<TurretInfo> simMidSlots, // 传入当前模拟的中间暂存区状态
        int maxSlotCapacity = 5, // 暂存区上限
        float difficulty = 0.3f)
    {
        // 1. 【最高优先级】：如果暴露的颜色中，有某种颜色恰好能清理掉“暂存区”里现有的残渣炮台，优先选择它！
        foreach (var color in exposedColors)
        {
            int midIndex = simMidSlots.FindIndex(t => t.color == color);
            if (midIndex != -1)
            {
                int exposedCount = GetExposedCountOfColor(simGrid, color);
                // 如果暴露的数量 >= 暂存区里该炮台剩余弹药，说明正向操作可以消灭暂存区的这个残渣，优先选它解套！
                if (exposedCount >= simMidSlots[midIndex].ammoCount)
                {
                    return color;
                }
            }
        }

        // 2. 筛选出【安全】的颜色（不会导致暂存区溢出）
        List<BlockColor> safeColors = new List<BlockColor>();

        foreach (var color in exposedColors)
        {
            int exposedCount = GetExposedCountOfColor(simGrid, color);

            // 判断如果选了这个颜色，正向玩的时候暂存区会不会增加新的占用
            bool willOccupyNewSlot = (exposedCount < 20); // 20是固定子弹数，如果打不完就会留在暂存区

            if (willOccupyNewSlot)
            {
                // 如果会占用新槽位，判断当前暂存区是否还有安全余量（比如限制最大只能积攒 3~4 个残渣，留 1 个缓冲）
                if (simMidSlots.Count < maxSlotCapacity - 1)
                {
                    safeColors.Add(color);
                }
            }
            else
            {
                // 如果能一次性全部消完（不占暂存区），绝对安全！
                safeColors.Add(color);
            }
        }

        // 3. 如果有安全颜色，在安全颜色里根据难度系数挑选
        List<BlockColor> candidateColors = safeColors.Count > 0 ? safeColors : exposedColors;

        // 按暴露数量【降序】排序（暴露多的排前面，利于消除）
        candidateColors.Sort((a, b) =>
        {
            int countA = GetExposedCountOfColor(simGrid, a);
            int countB = GetExposedCountOfColor(simGrid, b);
            return countB.CompareTo(countA);
        });

        // 根据难度计算选择索引
        int selectIndex = Mathf.FloorToInt(difficulty * (candidateColors.Count - 1));
        selectIndex = Mathf.Clamp(selectIndex, 0, candidateColors.Count - 1);

        return candidateColors[selectIndex];
    }

    private int GetExposedCountOfColor(BlockColor[,] simGrid, BlockColor color)
    {
        int count = 0;
        int cols = simGrid.GetLength(1);
        int rows = simGrid.GetLength(0);

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                if (simGrid[r, c] != BlockColor.Empty)
                {
                    if (simGrid[r, c] == color) count++;
                    break; // 只算底面暴露的第一层
                }
            }
        }
        return count;
    }

    private BlockColor GetMostExposedColor(BlockColor[,] simGrid, List<BlockColor> exposedColors)
    {
        BlockColor best = exposedColors[0];
        int maxCount = -1;
        foreach (var color in exposedColors)
        {
            int count = GetExposedCountOfColor(simGrid, color);
            if (count > maxCount)
            {
                maxCount = count;
                best = color;
            }
        }
        return best;
    }

    private void ClearCellsOfColor(BlockColor[,] simGrid, BlockColor color, int amount)
    {
        int cleared = 0;

        // 优先消除底部暴露的同色格
        for (int r = 0; r < rows && cleared < amount; r++)
        {
            for (int c = 0; c < cols && cleared < amount; c++)
            {
                if (simGrid[r, c] == color)
                {
                    simGrid[r, c] = BlockColor.Empty;
                    cleared++;
                }
            }
        }
    }

    private void ApplyGravity(BlockColor[,] simGrid)
    {
        for (int c = 0; c < cols; c++)
        {
            int writeRow = 0;
            for (int r = 0; r < rows; r++)
            {
                if (simGrid[r, c] != BlockColor.Empty)
                {
                    if (writeRow != r)
                    {
                        simGrid[writeRow, c] = simGrid[r, c];
                        simGrid[r, c] = BlockColor.Empty;
                    }
                    writeRow++;
                }
            }
        }
    }

    private bool IsGridEmpty(BlockColor[,] simGrid)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (simGrid[r, c] != BlockColor.Empty) return false;
        return true;
    }

    private BlockColor GetNeighborColor(int r, int c)
    {
        if (r > 0 && grid[r - 1, c] != BlockColor.Empty) return grid[r - 1, c];
        if (c > 0 && grid[r, c - 1] != BlockColor.Empty) return grid[r, c - 1];
        return BlockColor.Empty;
    }

    private BlockColor GetRandomColor()
    {
        return availableColors[Random.Range(0, availableColors.Count)];
    }

    #endregion
}