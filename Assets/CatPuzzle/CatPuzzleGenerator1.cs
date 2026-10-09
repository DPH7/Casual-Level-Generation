using System;
using System.Collections.Generic;
using System.Linq; // 引入 LINQ 支持 All、Select、Skip 等方法
using UnityEngine;

public class CatPuzzleGenerator1
{
    public class PuzzleResult
    {
        public int Size;
        public int[,] ColorGrid;
        public bool[,] Solution;
    }

    public double score = 0;
    
    public PuzzleResult GenerateLevel(int size, int colorCount, float difficulty)
    {
        int maxOuterCatAttempts = 200; // 外层：最多换 200 组猫
        int maxInnerColorAttempts = 100; // 内层：每组猫只试 50 次色块
        for (int catAttempt = 0; catAttempt < maxOuterCatAttempts; catAttempt++)
        {
            // 1. 生成一组新的猫位置
            bool[,] solution = GenerateCatPositions(size);
            if (solution == null) continue;

            // 2. 针对【色块扩散】和【唯一解校验】进行循环重试
            for (int attempt = 0; attempt < maxInnerColorAttempts; attempt++)
            {
                score = 0;
                
                // 基于同一套猫位置，生成不同的色块布局
                int[,] colorGrid = GenerateDivergentColorRegions(size, colorCount, solution);

                // 校验当前色块布局下，谜题是否有且仅有唯一解
                if (ValidateLevel(colorGrid, solution, size))
                {
                    List<string> catCoords = new List<string>();
                    for (int r = 0; r < size; r++)
                    {
                        for (int c = 0; c < size; c++)
                        {
                            if (solution[r, c])
                            {
                                catCoords.Add($"({r},{c})");
                            }
                        }
                    }

                    Debug.Log($"【生成猫位置】: {string.Join(" ", catCoords)}");
                    Debug.Log($"关卡生成成功! 尝试次数: {catAttempt * attempt + 1}");

                    Debug.Log($"关卡生成成功! 难度系数: {score}");
                    
                    return new PuzzleResult
                    {
                        Size = size,
                        ColorGrid = colorGrid,
                        Solution = solution
                    };
                }
            }
        }

        Debug.LogWarning("未能在此猫位置下生成合格色块，请重试或微调生成参数。");
        return null;
    }

    #region 双重校验机制 (唯一解 + 逻辑推导链)

    // 辅助深拷贝函数
    private int[,] CloneArray(int[,] source, int size)
    {
        int[,] clone = new int[size, size];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private bool[,] CloneArray(bool[,] source, int size)
    {
        bool[,] clone = new bool[size, size];
        Array.Copy(source, clone, source.Length);
        return clone;
    }
    
    private bool ValidateLevel(int[,] originalColorGrid, bool[,] originalSolution, int size)
    {
        // 检查生成的 ColorGrid 颜色种类是否严格等于 size
        HashSet<int> uniqueColors = new HashSet<int>();
        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            uniqueColors.Add(originalColorGrid[r, c]);

        if (uniqueColors.Count != size)
        {
            Debug.LogError($"色块生成异常！期望 {size} 种颜色，实际只有 {uniqueColors.Count} 种！");
        }
        
        // 1. 深度拷贝一份用于校验，防止推理算法修改原始色块数据
        int[,] colorGrid = CloneArray(originalColorGrid, size);
        bool[,] solution = CloneArray(originalSolution, size);

        // 校验 1：校验玩家是否能通过纯逻辑（无需猜测）推导出全盘
        if (!IsPuzzleDeductibleStrict(colorGrid, size))
        {
            return false;
        }

        return true;
    }

    #endregion

    #region 逻辑推导 Solver

    /// <summary>
    /// 严格推导校验：确保盘面从第一只猫开始，每一步都必须有明确的【单空】或【单线】线索
    /// </summary>
    public bool IsPuzzleDeductibleStrict(int[,] colorGrid, int size)
    {
        bool[,] possible = new bool[size, size];
        bool[,] catPositions = new bool[size, size];
        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            possible[r, c] = true;

        int solvedCats = 0;
        
        // 1. 寻找到第一个“开局独占色”（只有 1 格的初始颜色，例如红猫）
        for (int color = 0; color < size; color++)
        {
            var cells = GetValidCellsForColor(color, possible, colorGrid, size);
            if (cells.Count == 1)
            {
                PlaceCat(cells[0].x, cells[0].y, possible, catPositions, colorGrid, size);
                solvedCats++;
                break;
            }
        }

        if (solvedCats == 0) return false; // 没有确定的开局点，直接废弃关卡

        // 2. 纯单向确定性推导循环
        bool progress = true;
        while (progress && solvedCats < size)
        {
            progress = false;

            // ==========================================
            // 【前置规则 1】：区块限制（Pointing Elimination）
            // 若某个颜色的可用格完全集中在“同一行”或“同一列”，则该行/列的其他非本色空格绝不可能有猫
            // ==========================================
            for (int color = 0; color < size; color++)
            {
                if (HasCatInColor(color, catPositions, colorGrid, size)) continue;

                var cells = GetValidCellsForColor(color, possible, colorGrid, size);
                if (cells.Count == 0) continue;

                // 检查是否全部在同一行
                int targetRow = cells[0].x;
                bool sameRow = cells.All(p => p.x == targetRow);

                if (sameRow)
                {
                    for (int c = 0; c < size; c++)
                    {
                        // 该行非本颜色的空格全部排空
                        if (colorGrid[targetRow, c] != color && possible[targetRow, c])
                        {
                            possible[targetRow, c] = false;
                            progress = true;
                        }
                    }

                    if (progress)
                    {
                        score += 2;
                    }
                }

                // 检查是否全部在同一列
                int targetCol = cells[0].y;
                bool sameCol = cells.All(p => p.y == targetCol);

                if (sameCol)
                {
                    for (int r = 0; r < size; r++)
                    {
                        // 该列非本颜色的空格全部排空
                        if (colorGrid[r, targetCol] != color && possible[r, targetCol])
                        {
                            possible[r, targetCol] = false;
                            progress = true;
                        }
                    }
                    if (progress)
                    {
                        score += 2;
                    }
                }
            }

            if (progress) continue;

            // ==========================================
            // 【前置规则 2】：N 行/列隐性子集排除（N-Tuple Elimination）
            // 在未找猫的颜色中，若 N 个颜色的候选分布仅存在于指定的 N 行（或 N 列）内，
            // 则这 N 行（或 N 列）中属于“其他颜色”的格子绝不可能有猫
            // ==========================================
            if (ApplyNSubsetElimination(possible, catPositions, colorGrid, size))
            {
                progress = true;
                continue;
            }

            // 【法则 1】：检查某【颜色】是否只剩 1 个可用格
            for (int color = 0; color < size; color++)
            {
                var cells = GetValidCellsForColor(color, possible, colorGrid, size);
                // 如果该颜色还没放猫，且只剩 1 个空格可选 -> 必须是猫！
                if (cells.Count == 1 && !HasCatInColor(color, catPositions, colorGrid, size))
                {
                    score += 1;
                    PlaceCat(cells[0].x, cells[0].y, possible, catPositions, colorGrid, size);
                    solvedCats++;
                    progress = true;
                    break;
                }
            }

            if (progress) continue;

            // 【法则 2】：检查某【行】是否只剩 1 个可用格
            for (int r = 0; r < size; r++)
            {
                var cols = GetValidColsInRow(r, possible, size);
                if (cols.Count == 1 && !HasCatInRow(r, catPositions, size))
                {
                    score += 1;
                    PlaceCat(r, cols[0], possible, catPositions, colorGrid, size);
                    solvedCats++;
                    progress = true;
                    break;
                }
            }

            if (progress) continue;

            // 【法则 3】：检查某【列】是否只剩 1 个可用格
            for (int c = 0; c < size; c++)
            {
                var rows = GetValidRowsInCol(c, possible, size);
                if (rows.Count == 1 && !HasCatInCol(c, catPositions, size))
                {
                    score += 1;
                    PlaceCat(rows[0], c, possible, catPositions, colorGrid, size);
                    solvedCats++;
                    progress = true;
                    break;
                }
            }
        }

        // 最终校验：必须 N 只猫全清，且【每一个颜色区域都正好有 1 只猫】
        return solvedCats == size && IsAllColorsHasCat(colorGrid, catPositions, size);
    }

    /// <summary>
/// 规则 2 实现：检测 N 个未解颜色是否严格限制在 N 行/列中，并剔除这 N 行/列内其他颜色的可选项
/// </summary>
private bool ApplyNSubsetElimination(bool[,] possible, bool[,] catPositions, int[,] colorGrid, int size)
{
    // 收集所有未解出的颜色及其占用的行、列集合
    List<int> unplacedColors = new List<int>();
    Dictionary<int, HashSet<int>> colorRowsMap = new Dictionary<int, HashSet<int>>();
    Dictionary<int, HashSet<int>> colorColsMap = new Dictionary<int, HashSet<int>>();

    for (int color = 0; color < size; color++)
    {
        if (HasCatInColor(color, catPositions, colorGrid, size)) continue;

        var cells = GetValidCellsForColor(color, possible, colorGrid, size);
        if (cells.Count == 0) continue;

        unplacedColors.Add(color);
        colorRowsMap[color] = new HashSet<int>(cells.Select(p => p.x));
        colorColsMap[color] = new HashSet<int>(cells.Select(p => p.y));
    }

    bool modified = false;

    // 尝试遍历所有可能的 N 集合组合（从 N=2 到剩余未解决颜色数-1）
    int maxN = unplacedColors.Count;
    for (int n = 2; n < maxN; n++)
    {
        var combinations = GetCombinations(unplacedColors, n);

        foreach (var combo in combinations)
        {
            // 1. 行维度检测
            HashSet<int> combinedRows = new HashSet<int>();
            foreach (var colId in combo)
            {
                combinedRows.UnionWith(colorRowsMap[colId]);
            }

            // 若 N 个颜色仅分布在 N 行内
            if (combinedRows.Count == n)
            {
                foreach (int r in combinedRows)
                {
                    for (int c = 0; c < size; c++)
                    {
                        int cellColor = colorGrid[r, c];
                        // 这 N 行中，非当前 N 个组合颜色的其他所有格必须打叉
                        if (!combo.Contains(cellColor) && possible[r, c])
                        {
                            possible[r, c] = false;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    score += 1.5 * n;
                }
            }

            // 2. 列维度检测
            HashSet<int> combinedCols = new HashSet<int>();
            foreach (var colId in combo)
            {
                combinedCols.UnionWith(colorColsMap[colId]);
            }

            // 若 N 个颜色仅分布在 N 列内
            if (combinedCols.Count == n)
            {
                foreach (int c in combinedCols)
                {
                    for (int r = 0; r < size; r++)
                    {
                        int cellColor = colorGrid[r, c];
                        // 这 N 列中，非当前 N 个组合颜色的其他所有格必须打叉
                        if (!combo.Contains(cellColor) && possible[r, c])
                        {
                            possible[r, c] = false;
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    score += 1.5 * n;
                }
            }

            if (modified) return true;
        }
    }

    return modified;
}

/// <summary>
/// 组合生成器（用于枚举 N 个元素的组合）
/// </summary>
private List<List<T>> GetCombinations<T>(List<T> list, int length)
{
    if (length == 1) return list.Select(t => new List<T> { t }).ToList();

    var result = new List<List<T>>();
    for (int i = 0; i < list.Count; i++)
    {
        var head = list[i];
        var tailCombinations = GetCombinations(list.Skip(i + 1).ToList(), length - 1);
        foreach (var tail in tailCombinations)
        {
            var combo = new List<T> { head };
            combo.AddRange(tail);
            result.Add(combo);
        }
    }
    return result;
}
    
    /// <summary>
/// 获取指定颜色区域中所有仍可用的格子坐标列表
/// </summary>
private List<Vector2Int> GetValidCellsForColor(int targetColor, bool[,] possible, int[,] colorGrid, int size)
{
    List<Vector2Int> validCells = new List<Vector2Int>();
    for (int r = 0; r < size; r++)
    {
        for (int c = 0; c < size; c++)
        {
            if (colorGrid[r, c] == targetColor && possible[r, c])
            {
                validCells.Add(new Vector2Int(r, c));
            }
        }
    }
    return validCells;
}

/// <summary>
/// 检查指定颜色区域内是否已经放置了猫
/// </summary>
private bool HasCatInColor(int targetColor, bool[,] catPositions, int[,] colorGrid, int size)
{
    for (int r = 0; r < size; r++)
    {
        for (int c = 0; c < size; c++)
        {
            if (colorGrid[r, c] == targetColor && catPositions[r, c])
            {
                return true;
            }
        }
    }
    return false;
}

/// <summary>
/// 获取指定行中所有仍可用的列号列表
/// </summary>
private List<int> GetValidColsInRow(int row, bool[,] possible, int size)
{
    List<int> validCols = new List<int>();
    for (int c = 0; c < size; c++)
    {
        if (possible[row, c])
        {
            validCols.Add(c);
        }
    }
    return validCols;
}

/// <summary>
/// 检查指定行中是否已经放置了猫
/// </summary>
private bool HasCatInRow(int row, bool[,] catPositions, int size)
{
    for (int c = 0; c < size; c++)
    {
        if (catPositions[row, c]) return true;
    }
    return false;
}

/// <summary>
/// 获取指定列中所有仍可用的行号列表
/// </summary>
private List<int> GetValidRowsInCol(int col, bool[,] possible, int size)
{
    List<int> validRows = new List<int>();
    for (int r = 0; r < size; r++)
    {
        if (possible[r, col])
        {
            validRows.Add(r);
        }
    }
    return validRows;
}

/// <summary>
/// 检查指定列中是否已经放置了猫
/// </summary>
private bool HasCatInCol(int col, bool[,] catPositions, int size)
{
    for (int r = 0; r < size; r++)
    {
        if (catPositions[r, col]) return true;
    }
    return false;
}

private void PlaceCat(int r, int c, bool[,] possible, bool[,] catPositions, int[,] colorGrid, int size)
{
    catPositions[r, c] = true;
    int color = colorGrid[r, c];

    for (int i = 0; i < size; i++)
    {
        for (int j = 0; j < size; j++)
        {
            // 同行、同列、同颜色打叉
            if (i == r || j == c || colorGrid[i, j] == color)
            {
                possible[i, j] = false;
            }
            // 8 连通打叉
            if (Mathf.Abs(i - r) <= 1 && Mathf.Abs(j - c) <= 1)
            {
                possible[i, j] = false;
            }
        }
    }
}

private bool IsAllColorsHasCat(int[,] colorGrid, bool[,] catPositions, int size)
{
    HashSet<int> colorsWithCat = new HashSet<int>();
    for (int r = 0; r < size; r++)
    {
        for (int c = 0; c < size; c++)
        {
            if (catPositions[r, c])
            {
                int color = colorGrid[r, c];
                if (colorsWithCat.Contains(color)) return false; // 单个颜色放了多只猫，非法
                colorsWithCat.Add(color);
            }
        }
    }
    return colorsWithCat.Count == size; // 必须 N 个颜色都有猫
}
    #endregion

    #region 发散不规则地图填充

    private int[,] GenerateDivergentColorRegions(int size, int colorCount, bool[,] solution)
    {
        int[,] grid = new int[size, size];
        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            grid[r, c] = -1;

        List<Vector2Int> catPositions = new List<Vector2Int>();
        for (int r = 0; r < size; r++)
        for (int c = 0; c < size; c++)
            if (solution[r, c])
                catPositions.Add(new Vector2Int(r, c));

        // 1. 开局线索：0号颜色仅留1格（猫所在位置）
        grid[catPositions[0].x, catPositions[0].y] = 0;

        // 2. 初始化其余颜色的 BFS 队列
        List<Queue<Vector2Int>> colorQueues = new List<Queue<Vector2Int>>();
        for (int i = 0; i < colorCount; i++) colorQueues.Add(new Queue<Vector2Int>());

        for (int i = 1; i < colorCount; i++)
        {
            Vector2Int pos = catPositions[i];
            grid[pos.x, pos.y] = i;
            colorQueues[i].Enqueue(pos);
        }

        // 3. 严格四连通泛滥扩充
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        bool hasUnassigned = true;

        while (hasUnassigned)
        {
            hasUnassigned = false;
            for (int cId = 1; cId < colorCount; cId++)
            {
                if (colorQueues[cId].Count == 0) continue;

                Vector2Int curr = colorQueues[cId].Dequeue();
                List<Vector2Int> dirs = new List<Vector2Int>(directions);
                ShuffleList(dirs);

                foreach (var dir in dirs)
                {
                    Vector2Int next = curr + dir;
                    if (next.x >= 0 && next.x < size && next.y >= 0 && next.y < size && grid[next.x, next.y] == -1)
                    {
                        grid[next.x, next.y] = cId;
                        colorQueues[cId].Enqueue(next);
                        hasUnassigned = true;
                        break;
                    }
                }
            }

            // 强行清理剩余空格
            if (!hasUnassigned)
            {
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        if (grid[r, c] == -1)
                        {
                            List<int> neighbors = Get4WayNeighborColors(new Vector2Int(r, c), grid, size);
                            neighbors.RemoveAll(id => id == 0); // 绝不破坏0号颜色的单格特性
                            if (neighbors.Count > 0)
                            {
                                int selected = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                                grid[r, c] = selected;
                                colorQueues[selected].Enqueue(new Vector2Int(r, c));
                                hasUnassigned = true;
                            }
                        }
                    }
                }
            }
        }

        // 4. 安全破坏 2x2（带有四连通保护判定）
        //Break2x2BlocksSafe(grid, size);

        return grid;
    }


/// <summary>
/// 辅助方法：仅获取【上下左右】四方向邻居颜色
/// </summary>
private List<int> Get4WayNeighborColors(Vector2Int pos, int[,] grid, int size)
{
    List<int> colors = new List<int>();
    Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    foreach (var dir in dirs)
    {
        Vector2Int next = pos + dir;
        if (next.x >= 0 && next.x < size && next.y >= 0 && next.y < size && grid[next.x, next.y] != -1)
        {
            if (!colors.Contains(grid[next.x, next.y]))
            {
                colors.Add(grid[next.x, next.y]);
            }
        }
    }
    return colors;
}

    private bool[,] GenerateCatPositions(int size)
    {
        bool[,] board = new bool[size, size];
        if (PlaceCatDFS(0, board, size))
            return board;
        return null;
    }

    private bool PlaceCatDFS(int row, bool[,] board, int size)
    {
        if (row == size) return true;

        List<int> cols = new List<int>();
        for (int c = 0; c < size; c++) cols.Add(c);
        ShuffleList(cols);

        foreach (int col in cols)
        {
            if (CanPlaceCat(row, col, board, size))
            {
                board[row, col] = true;
                if (PlaceCatDFS(row + 1, board, size)) return true;
                board[row, col] = false;
            }
        }
        return false;
    }

    private bool CanPlaceCat(int r, int c, bool[,] board, int size)
    {
        for (int i = 0; i < size; i++)
        {
            if (board[i, c]) return false;
            if (board[r, i]) return false;
        }

        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int i = 0; i < 8; i++)
        {
            int nr = r + dr[i];
            int nc = c + dc[i];
            if (nr >= 0 && nr < size && nc >= 0 && nc < size)
            {
                if (board[nr, nc]) return false;
            }
        }
        return true;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    #endregion
}