using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridVisualizer : MonoBehaviour
{
    [Header("依赖组件")]
    public LevelAndTurretGenerator levelGenerator;

    [Header("关卡矩阵 UI 配置")]
    public Transform gridContainer;       // 上方关卡色块容器 (挂载 GridLayoutGroup)
    public GameObject blockPrefab;        // 色块 Prefab (带 Image)

    [Header("炮台队列 UI 配置")]
    public Transform turretContainer;     // 下方炮台队列容器 (挂载 HorizontalLayoutGroup)
    public GameObject turretPrefab;       // 炮台 Prefab (包含 Image 背景、Text 弹药数 和 Button)
    
    [Header("中间 UI 配置")]
    public Transform midContainer;     // 下方炮台队列容器 (挂载 HorizontalLayoutGroup)
    
    [Header("颜色映射表")]
    public Color blackColor = Color.black;
    public Color yellowColor = Color.yellow;
    public Color whiteColor = Color.white;
    public Color redColor = Color.red;
    public Color greenColor = Color.green;
    public Color blueColor = Color.blue;
    public Color cyanColor = Color.cyan;
    public Color magentaColor = Color.magenta;
    public Color orangeColor = new Color(1f, 0.5f, 0f); // 橙色 RGBA

    private Dictionary<LevelAndTurretGenerator.BlockColor, Color> colorMap;

    // 运行时的矩阵数据与炮台队列
    private LevelAndTurretGenerator.BlockColor[,] currentGrid;
    // 运行时的炮台二维矩阵
    private LevelAndTurretGenerator.TurretInfo[,] currentTurretMatrix;
    
    // 1. 在变量声明区域，新增中间炮架的数据列表
    private List<LevelAndTurretGenerator.TurretInfo> midTurretList = new List<LevelAndTurretGenerator.TurretInfo>();

    private void Awake()
    {
        InitColorMap();
    }

    private void Start()
    {
        VisualizeLevelAndTurrets();
    }

    private void InitColorMap()
    {
        colorMap = new Dictionary<LevelAndTurretGenerator.BlockColor, Color>
        {
            { LevelAndTurretGenerator.BlockColor.Black, blackColor },
            { LevelAndTurretGenerator.BlockColor.Yellow, yellowColor },
            { LevelAndTurretGenerator.BlockColor.White, whiteColor },
            { LevelAndTurretGenerator.BlockColor.Red, redColor },
            { LevelAndTurretGenerator.BlockColor.Green, greenColor },
            { LevelAndTurretGenerator.BlockColor.Blue, blueColor },
            { LevelAndTurretGenerator.BlockColor.Cyan, cyanColor },
            { LevelAndTurretGenerator.BlockColor.Magenta, magentaColor },
            { LevelAndTurretGenerator.BlockColor.Orange, orangeColor }
        };
    }

    [ContextMenu("重新生成并渲染")]
    public void VisualizeLevelAndTurrets()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("请先在 Inspector 中赋值 LevelAndTurretGenerator！");
            return;
        }

        InitColorMap();

        // 1. 生成并保存运行时数据
        
        currentGrid = levelGenerator.GenerateLevelAndTurrets();
        currentTurretMatrix = levelGenerator.ReverseSolveTurretMatrix();

        // int maxCnt = 1000, count = 0;
        // while (currentTurretMatrix == null && count < maxCnt)
        // {
        //     count++;
        //     currentGrid = levelGenerator.GenerateLevelAndTurrets();
        //     currentTurretMatrix = levelGenerator.ReverseSolveTurretMatrix();
        // }
        
        midTurretList.Clear(); // 重新生成时清空中间槽
        
        // 2. 渲染 UI
        if (currentTurretMatrix != null)
        {
            RefreshAllUI();
        }
    }

    /// <summary>
    /// 刷新上下所有 UI 视图
    /// </summary>
    private void RefreshAllUI()
    {
        DrawLevelGrid();
        DrawTurretQueue();
        DrawMidTurretQueue(); // 新增：渲染中间炮架
    }

    #region 渲染网格

    private void DrawLevelGrid()
    {
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        int rows = currentGrid.GetLength(0);
        int cols = currentGrid.GetLength(1);

        // 按 Start Corner = Lower Left 排列（r=0 在最下）
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject blockObj = Instantiate(blockPrefab, gridContainer);
                Image img = blockObj.GetComponent<Image>();

                LevelAndTurretGenerator.BlockColor cellColor = currentGrid[r, c];
                if (img != null && colorMap.TryGetValue(cellColor, out Color color))
                {
                    img.color = color;
                }
            }
        }
    }

    #endregion

    #region 渲染与点击炮台

    /// <summary>
/// 渲染下方 N 列的炮台矩阵 (Start Corner = Lower Left)
/// </summary>
private void DrawTurretQueue()
{
    if (turretContainer == null) return;

    foreach (Transform child in turretContainer)
    {
        Destroy(child.gameObject);
    }

    int rows = currentTurretMatrix.GetLength(0);
    int cols = currentTurretMatrix.GetLength(1);

    // 设置容器 GridLayoutGroup 的 Constraint 为 Fixed Column Count = cols
    GridLayoutGroup layout = turretContainer.GetComponent<GridLayoutGroup>();
    if (layout != null)
    {
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = cols;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft; // 保证 r=0 在最下方
    }

    // 从下往上 (r=0 -> rows-1)、从左到右 (c=0 -> cols-1) 渲染
    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            var turretInfo = currentTurretMatrix[r, c];
            
            // 如果为空点（最后一行没填满的位置），实例化透明不可点对象或直接 skip
            if (turretInfo.ammoCount <= 0) 
            {
                GameObject emptyObj = Instantiate(blockPrefab, turretContainer);
                Image emptyImg = emptyObj.GetComponent<Image>();
                if (emptyImg != null) emptyImg.enabled = false;
                continue;
            }

            int rowIdx = r;
            int colIdx = c;

            GameObject turretObj = Instantiate(turretPrefab, turretContainer);

            // 设置颜色与弹药
            Image img = turretObj.GetComponent<Image>();
            if (img == null) img = turretObj.GetComponentInChildren<Image>();
            if (img != null && colorMap.TryGetValue(turretInfo.color, out Color color))
            {
                img.color = color;
            }

            Text ammoText = turretObj.GetComponentInChildren<Text>();
            if (ammoText != null)
            {
                ammoText.text = turretInfo.ammoCount.ToString();
            }

            // 绑定点击事件
            Button btn = turretObj.GetComponent<Button>();
            if (btn == null) btn = turretObj.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTurretMatrixClicked(rowIdx, colIdx));
        }
    }
}
    
    private void DrawMidTurretQueue()
    {
        if (midContainer == null) return;

        foreach (Transform child in midContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < midTurretList.Count; i++)
        {
            int index = i;
            var turretInfo = midTurretList[index];
            GameObject turretObj = Instantiate(turretPrefab, midContainer);

            // 设置颜色与弹药
            Image img = turretObj.GetComponent<Image>();
            if (img == null) img = turretObj.GetComponentInChildren<Image>();
            if (img != null && colorMap.TryGetValue(turretInfo.color, out Color color))
            {
                img.color = color;
            }

            Text ammoText = turretObj.GetComponentInChildren<Text>();
            if (ammoText != null)
            {
                ammoText.text = turretInfo.ammoCount.ToString();
            }

            // 绑定中间炮架点击事件
            Button btn = turretObj.GetComponent<Button>();
            if (btn == null) btn = turretObj.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnMidTurretClicked(index));
        }
    }
    

    /// <summary>
    /// 点击二维炮台矩阵的逻辑
    /// </summary>
    private void OnTurretMatrixClicked(int r, int c)
    {
        var turret = currentTurretMatrix[r, c];
        if (turret.ammoCount <= 0) return;

        // 消除逻辑
        int clearedCount = ExecuteElimination(turret.color, turret.ammoCount);
        ApplyGravity();

        // 标记该位置已被使用/置空
        currentTurretMatrix[r, c] = new LevelAndTurretGenerator.TurretInfo(LevelAndTurretGenerator.BlockColor.Empty, 0);
        ApplyTurretMatrixGravity();
        // 如果未完全消除，转移到中间炮架
        if (clearedCount < turret.ammoCount)
        {
            turret.ammoCount -= clearedCount;
            midTurretList.Add(turret);
        }

        RefreshAllUI();
    }
    
    // 5. 新增：点击中间炮架炮台的逻辑
    private void OnMidTurretClicked(int midIndex)
    {
        if (midIndex < 0 || midIndex >= midTurretList.Count) return;

        var turret = midTurretList[midIndex];

        int clearedCount = ExecuteElimination(turret.color, turret.ammoCount);
        ApplyGravity();

        if (clearedCount == turret.ammoCount)
        {
            // 弹药全打完，从中间炮架移除
            midTurretList.RemoveAt(midIndex);
        }
        else if (clearedCount > 0)
        {
            // 扣减剩余弹药，留在中间炮架
            turret.ammoCount -= clearedCount;
            midTurretList[midIndex] = turret;
        }

        RefreshAllUI();
    }

    /// <summary>
    /// 从左到右、从下到上 消除底部暴露的同色格子（无重力，直接露出生长在上面的格）
    /// </summary>
    private int ExecuteElimination(LevelAndTurretGenerator.BlockColor targetColor, int maxAmmo)
    {
        int rows = currentGrid.GetLength(0);
        int cols = currentGrid.GetLength(1);

        int ammoLeft = maxAmmo;
        bool foundAnyInLoop = true;

        // 只要还有弹药，且上一轮能找到可消除的底格，就继续消除
        while (ammoLeft > 0 && foundAnyInLoop)
        {
            foundAnyInLoop = false;

            // 从左到右遍历每一列
            for (int c = 0; c < cols && ammoLeft > 0; c++)
            {
                // 从下到上寻找该列第一个非空格子（即最底部的暴露格）
                for (int r = 0; r < rows; r++)
                {
                    if (currentGrid[r, c] != LevelAndTurretGenerator.BlockColor.Empty)
                    {
                        // 如果最底部的暴露格颜色符合，进行消除
                        if (currentGrid[r, c] == targetColor)
                        {
                            currentGrid[r, c] = LevelAndTurretGenerator.BlockColor.Empty;
                            ammoLeft--;
                            foundAnyInLoop = true;
                        }
                        // 找到该列底部第一个格子后（无论是否同色），跳出该列的纵向查找，进入下一列
                        break; 
                    }
                }
            }
        }

        return maxAmmo - ammoLeft;
    }
    
    private void ApplyGravity()
    {
        int rows = currentGrid.GetLength(0);
        int cols = currentGrid.GetLength(1);

        for (int c = 0; c < cols; c++)
        {
            int writeRow = 0;
            for (int r = 0; r < rows; r++)
            {
                if (currentGrid[r, c] != LevelAndTurretGenerator.BlockColor.Empty)
                {
                    if (writeRow != r)
                    {
                        currentGrid[writeRow, c] = currentGrid[r, c];
                        currentGrid[r, c] = LevelAndTurretGenerator.BlockColor.Empty;
                    }
                    writeRow++;
                }
            }
        }
    }
    
    private void ApplyTurretMatrixGravity()
    {
        if (currentTurretMatrix == null) return;

        int rows = currentTurretMatrix.GetLength(0);
        int cols = currentTurretMatrix.GetLength(1);

        for (int c = 0; c < cols; c++)
        {
            int writeRow = 0;
            for (int r = 0; r < rows; r++)
            {
                // 判断当前槽位是否有有效炮台（弹药量 > 0 且颜色非 Empty）
                if (currentTurretMatrix[r, c].ammoCount > 0 && 
                    currentTurretMatrix[r, c].color != LevelAndTurretGenerator.BlockColor.Empty)
                {
                    if (writeRow != r)
                    {
                        // 将上方 valid 的炮台向下平移补位（填充到 writeRow 位置，即优先填充 r = 0）
                        currentTurretMatrix[writeRow, c] = currentTurretMatrix[r, c];
                        currentTurretMatrix[r, c] = new LevelAndTurretGenerator.TurretInfo(LevelAndTurretGenerator.BlockColor.Empty, 0);
                    }
                    writeRow++;
                }
            }
        }
    }

    #endregion
}