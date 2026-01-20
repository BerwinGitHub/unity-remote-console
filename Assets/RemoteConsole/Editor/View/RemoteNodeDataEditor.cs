using UnityEditor;
using UnityEngine;
using RConsole.Runtime;

namespace RConsole.Editor
{
    [CustomEditor(typeof(RemoteNodeData))]
    public class RemoteNodeDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var data = (RemoteNodeData)target;

            if (data.Components == null || data.Components.Count == 0)
            {
                EditorGUILayout.HelpBox("没有组件数据", MessageType.Info);
                return;
            }

            foreach (var comp in data.Components)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(comp.TypeName, EditorStyles.boldLabel);

                if (comp.Properties != null)
                {
                    foreach (var kv in comp.Properties)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kv.Key, GUILayout.Width(120));
                        EditorGUILayout.TextField(kv.Value);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (comp.ExtraData != null && comp.ExtraData.Length > 0)
                {
                    var tex = new Texture2D(2, 2);
                    if (tex.LoadImage(comp.ExtraData))
                    {
                        GUILayout.Space(5);
                        var aspect = (float)tex.width / tex.height;
                        var width = Mathf.Min(tex.width, 200);
                        var height = width / aspect;
                        GUILayout.Label(tex, GUILayout.Width(width), GUILayout.Height(height));
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
    }
}
