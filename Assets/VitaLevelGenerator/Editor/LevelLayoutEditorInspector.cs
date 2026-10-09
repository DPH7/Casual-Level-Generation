#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
[CustomEditor(typeof(LevelLayoutEditor))]
public class LevelLayoutEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelLayoutEditor mgr = (LevelLayoutEditor)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎮 关卡手工搭建工具", EditorStyles.boldLabel);

        if (GUILayout.Button("🌱 生成第一张起点牌 (Start Tile)", GUILayout.Height(30)))
        {
            mgr.CreateTileAt(0, 0, 0);
        }
    }
}
#endif