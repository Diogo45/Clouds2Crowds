using UnityEngine;
using UnityEditor;

namespace BioCrowds
{
#if(UNITY_EDITOR)
    public class BioCrowdsPreferences
    {

        // The Preferences
        private static bool quadtreeActive = true;
        private static bool spawnAgentsStructured = true;
        private static int heigth = 4;
        private static float waitFor = 15f;

        [PreferenceItem("BioCrowds")]
        private static void CustomPreferencesGUI()
        {

            EditorGUILayout.Toggle("Quadtree Active: ", quadtreeActive);
            EditorGUILayout.IntField("  Quadtree Heigth: ", heigth, GUIStyle.none, null);

            EditorGUILayout.FloatField(" Wait seconds to Spawn Agents:", waitFor, GUIStyle.none, null);

            EditorGUILayout.Toggle("Spawn Agents Structured: ", spawnAgentsStructured);
            //if (GUI.changed)
            //{
            //    EditorPrefs.SetBool("BoolPreferenceKey", boolPreference);
            //}
        }
    }
#endif
}
