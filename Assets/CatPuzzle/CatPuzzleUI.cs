using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatPuzzleUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform boardContainer; // 格子父节点
    [SerializeField] private GameObject cellPrefab;        // 格子预制体
    [SerializeField] private Sprite catSprite;             // 猫咪图标 (没有可留空，默认使用色块)

    [Header("配色方案 (Palette)")]
    [SerializeField] private List<Color> palette = new List<Color>
    {
        new Color(0.85f, 0.65f, 0.13f), // 黄色
        new Color(0.55f, 0.35f, 0.20f), // 棕色
        new Color(0.25f, 0.45f, 0.65f), // 深蓝
        new Color(0.55f, 0.70f, 0.85f), // 浅蓝
        new Color(0.45f, 0.75f, 0.45f), // 绿
        new Color(0.85f, 0.50f, 0.65f), // 粉红
        new Color(0.60f, 0.45f, 0.75f), // 紫
        new Color(0.90f, 0.55f, 0.35f)  // 橙
    };

    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
    }

    /// <summary>
    /// 渲染盘面主入口
    /// </summary>
    public void RenderPuzzle(CatPuzzleGenerator1.PuzzleResult puzzle)
    {
        ClearBoard();
        SetupGridContainer(puzzle.Size);

        // 如果你的 row = 0 对应数组底部，按屏幕从上到下渲染时，需要从 Size-1 递减到 0
        for (int r = 0; r < puzzle.Size; r++)
        {
            for (int c = 0; c < puzzle.Size; c++)
            {
                int colorId = puzzle.ColorGrid[r, c];
                bool hasCat = puzzle.Solution[r, c];
                
                // 检查：确保 CreateCell(row, col) 内部没有把 row 和 col 用反！
                // 行(r) 对应垂直 Y，列(c) 对应水平 X
                CreateCell(row: r, col: c, colorId, hasCat);
            }
        }
    }
    /// <summary>
    /// 动态设置 GridLayoutGroup 的尺寸与间距，保持盘面响应式缩放
    /// </summary>
    private void SetupGridContainer(int size)
    {
        float containerSize = boardContainer.rect.width; // 假设容器为正方形
        float spacing = 6f;                              // 格子间距
        float totalSpacing = spacing * (size - 1);
        float cellSize = (containerSize - totalSpacing) / size;

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = size;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
    }

    private void CreateCell(int row, int col, int colorId, bool hasCat)
    {
        GameObject cellObj = Instantiate(cellPrefab, boardContainer);
        GridCellUI cellUI = cellObj.GetComponent<GridCellUI>();
        if (cellUI != null)
        {
            Color cellColor = GetColorFromID(colorId);
            cellUI.Init(cellColor, hasCat, catSprite);
        }
    }

    private Color GetColorFromID(int id, int totalColors = 10)
    {
        // 高对比度预设色板 (针对 10 色以内的常用色彩)
        Color[] distinctPalette = new Color[]
        {
            new Color(0.92f, 0.25f, 0.20f), // 1. 鲜红
            new Color(0.18f, 0.52f, 0.88f), // 2. 湛蓝
            new Color(0.95f, 0.70f, 0.10f), // 3. 亮黄
            new Color(0.20f, 0.72f, 0.35f), // 4. 鲜绿
            new Color(0.60f, 0.30f, 0.85f), // 5. 紫色
            new Color(0.95f, 0.45f, 0.15f), // 6. 橙色
            new Color(0.15f, 0.75f, 0.75f), // 7. 青色
            new Color(0.90f, 0.40f, 0.65f), // 8. 粉红
            new Color(0.50f, 0.30f, 0.15f), // 9. 棕色
            new Color(0.40f, 0.50f, 0.60f)  // 10. 灰蓝
        };

        if (id >= 0 && id < distinctPalette.Length)
        {
            return distinctPalette[id];
        }

        // 若超出预设数量，采用 HSV 黄金分割算法计算互补高对比度色彩
        float goldenRatio = 0.618033988749895f;
        float h = (id * goldenRatio) % 1.0f;
        return Color.HSVToRGB(h, 0.75f, 0.85f); // 保持高饱和度与亮度
    }

    private void ClearBoard()
    {
        foreach (Transform child in boardContainer)
        {
            Destroy(child.gameObject);
        }
    }
}