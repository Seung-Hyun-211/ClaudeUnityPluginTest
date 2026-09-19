using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Items;

namespace Game.EditorTools
{
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Collect all ItemData in project"))
            {
                Collect();
            }

            var problems = ((ItemDatabase)target).FindProblems();
            if (problems.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", problems), MessageType.Warning);
            }
        }

        private void Collect()
        {
            var found = AssetDatabase.FindAssets("t:ItemData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null)
                .OrderBy(item => item.ItemId)
                .ToArray();

            var so = new SerializedObject(target);
            var items = so.FindProperty("items");
            items.arraySize = found.Length;
            for (int i = 0; i < found.Length; i++)
            {
                items.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }
            so.ApplyModifiedProperties();
        }
    }
}
