using UnityEngine;

public class GameManager2 : MonoBehaviour
{
    private CatPuzzleGenerator1 generator = new CatPuzzleGenerator1();
    [SerializeField] private CatPuzzleUI puzzleUI;

    [Header("测试参数")]
    [Range(4, 12)] public int boardSize = 8;
    [Range(0.0f, 1.0f)] public float difficulty = 0.5f;

    private void Start()
    {
        GenerateAndDraw();
    }

    [ContextMenu("重新生成并绘制关卡")]
    public void GenerateAndDraw()
    {
        // 1. 调用生成算法 (颜色数默认等于 size)
        var result = generator.GenerateLevel(boardSize, boardSize, difficulty);

        // 2. 渲染盘面
        if (result != null)
        {
            puzzleUI.RenderPuzzle(result);
        }
    }
}