using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(BoidsManager))]
    public class BoidsManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            BoidsManager boidsManager = (BoidsManager)target;
 
            // if(GUILayout.Button("Test sorting"))
            // {
            // }
        }
    }
}