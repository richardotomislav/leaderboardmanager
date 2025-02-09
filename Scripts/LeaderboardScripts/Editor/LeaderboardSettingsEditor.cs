using System.Linq;
using System.Reflection;
using LeaderboardCreatorEditor;
using LeaderbordManager;

namespace LeaderboardScripts.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(LeaderboardSettings))]
    public class LeaderboardSettingsEditor : Editor
    {
        private bool showKeys = false;

        public override void OnInspectorGUI()
        {
            var settings = (LeaderboardSettings)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Leaderboards", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            showKeys = EditorGUILayout.Toggle("Show Keys", showKeys);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Leaderboard Creator"))
            {
                EditorApplication.ExecuteMenuItem("Leaderboard Creator/My Leaderboards");
            }

            if (GUILayout.Button("Refresh Leaderboards"))
            {
                settings.RefreshLeaderboards();

                var windowType = typeof(LeaderboardCreatorWindow);
                var method = windowType.GetMethod("SaveLeaderboardsToScript",
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (method != null)
                {
                    method.Invoke(null, null);
                }
                else
                {
                    Debug.LogWarning("Could not find SaveLeaderboardsToScript method");
                }
            }

            EditorGUILayout.Space(10);

            if (settings.leaderboards != null && settings.leaderboards.Any())
            {
                foreach (var entry in settings.leaderboards)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("Name", entry.name);

                    if (showKeys)
                    {
                        var field = typeof(Dan.Main.Leaderboards).GetField(entry.name);
                        if (field != null)
                        {
                            var leaderboardRef = field.GetValue(null) as Dan.Main.LeaderboardReference;
                            if (leaderboardRef != null)
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField("Public Key", leaderboardRef.PublicKey);
                                EditorGUI.EndDisabledGroup();
                            }
                        }
                    }

                    entry.type = (LeaderboardType)EditorGUILayout.EnumPopup("Type", entry.type);

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No leaderboards found. Add leaderboards through the Leaderboard Creator window.",
                    MessageType.Info);
            }

            var unsetTypes = settings.leaderboards?.Where(l => l.type == LeaderboardType.None).ToList();
            if (unsetTypes != null && unsetTypes.Any())
            {
                var names = string.Join(", ", unsetTypes.Select(l => l.name));
                EditorGUILayout.HelpBox(
                    $"The following leaderboards need their type configured: {names}",
                    MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.HelpBox(
                "If no new leaderboards appear, remember to 'Save to C# Script' in the Leaderboard Creator window. Or press 'Refresh Leaderboards'",MessageType.Info);
            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
