using UnityEngine;
using UnityEditor;

namespace UnityEditorExtension
{
    public static class GUIToggle
    {
        private const int WIDTH = 16;
        private const int OFFSET = 10;

        [MenuItem(Config.ExtensionMenuPath + nameof(GUIToggle))]
        private static void Initialize()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
        }

        private static void OnGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
            {
                return;
            }

            var pos = selectionRect;
            pos.x = pos.xMax - OFFSET;
            pos.width = WIDTH;

            bool active = GUI.Toggle(pos, go.activeSelf, string.Empty);
            if (active == go.activeSelf)
            {
                return;
            }
            Undo.RecordObject(go, $"{(active ? "Activate" : "Deactivate")} GameObject '{go.name}'");
            go.SetActive(active);
            EditorUtility.SetDirty(go);
        }
    }
}
