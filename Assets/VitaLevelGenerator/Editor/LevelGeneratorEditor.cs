#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
[CustomEditor(typeof(LevelVisualizer))]
public class LevelGeneratorEditor : Editor
{
    private int maxLayers = 4;
    private int baseWidth = 5;
    private int baseHeight = 5;
    private Difficulty difficulty = Difficulty.Hard;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelVisualizer visualizer = (LevelVisualizer)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎮 关卡实体生成面板", EditorStyles.boldLabel);

        maxLayers = EditorGUILayout.IntSlider("最大层数 (Max Layers)", maxLayers, 1, 8);
        baseWidth = EditorGUILayout.IntSlider("基础宽度 (Width)", baseWidth, 2, 10);
        baseHeight = EditorGUILayout.IntSlider("基础高度 (Height)", baseHeight, 2, 10);
        difficulty = (Difficulty)EditorGUILayout.EnumPopup("算法难度", difficulty);

        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("2. 填牌验证并渲染结果", GUILayout.Height(35)))
        {
            LevelGenerator generator = new LevelGenerator();
            ForwardSolver solver = new ForwardSolver();
            List<Slot> runtimeSlots = LevelDataExtractor.ExtractRuntimeSlots(visualizer.transform);
            visualizer.currentLayout = runtimeSlots;
            
            bool success = false;
            var pairSlot = generator.GenerateLevel(runtimeSlots);
            // generator.ColorLevel(pairSlot, runtimeSlots, System.Enum.GetValues(typeof(TileType))
            //     .Cast<TileType>()
            //     .ToList());
            for (var i = 0; i < pairSlot.Count; i++)
            {
                var slot = pairSlot[i];
                var name = $"Tile_{slot.SlotA.Id}_L{slot.SlotA.Layer}";
                var tileObj = visualizer.transform.Find(name);
                if (tileObj)
                {
                    // 数字标签
                    visualizer.CreateTextLabel(tileObj.transform, i);
                }
                    
                var name1 = $"Tile_{slot.SlotB.Id}_L{slot.SlotB.Layer}";
                var tileObj1 = visualizer.transform.Find(name1);
                if (tileObj1)
                {
                    // 数字标签
                    visualizer.CreateTextLabel(tileObj1.transform, i);
                }
            }
            //visualizer.RenderBoard(); // 更新实体方块上的文字/图案信息
        }

        if (GUILayout.Button("清空实体场景", GUILayout.Height(25)))
        {
            visualizer.currentLayout.Clear();
            visualizer.ClearBoard();
        }
    }
}
#endif