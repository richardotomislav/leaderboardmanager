
using System.Linq;
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
            // Add a toggle for showing keys
            // Add horizontal group for controls
            EditorGUILayout.BeginHorizontal();

            // Toggle for showing keys
            showKeys = EditorGUILayout.Toggle("Show Keys", showKeys);

            // Add refresh button
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                settings.RefreshLeaderboards();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            // Add button to open LeaderboardCreator window
            if (GUILayout.Button("Open Leaderboard Creator"))
            {
                EditorApplication.ExecuteMenuItem("Leaderboard Creator/My Leaderboards");
            }

            EditorGUILayout.Space(10);

            // Rest of your inspector code...
            if (settings.leaderboards != null && settings.leaderboards.Any())
            {
                foreach (var entry in settings.leaderboards)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Show leaderboard name (read-only)
                    EditorGUILayout.LabelField("Name", entry.name);

                    // Show keys if toggle is on
                    if (showKeys)
                    {
                        // Get reference from Leaderboards class
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

                    // Allow type modification
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


            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
