using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using LeaderbordManager;

[CreateAssetMenu(fileName = "LeaderboardSettings", menuName = "Leaderboards/Settings")]
public class LeaderboardSettings : ScriptableObject
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public LeaderboardType type;
    }

    public List<LeaderboardEntry> leaderboards = new List<LeaderboardEntry>();

    public void RefreshLeaderboards()
    {
        Debug.Log($"[LeaderboardSettings] Starting refresh...");
        var fields = typeof(Dan.Main.Leaderboards).GetFields(BindingFlags.Public | BindingFlags.Static);

        // Create a dictionary of existing types
        var existingTypes = leaderboards.ToDictionary(
            entry => entry.name,
            entry => entry.type
        );

        leaderboards.Clear();

        foreach(var field in fields)
        {
            // Use existing type if available, otherwise default to Basic
            var type = existingTypes.ContainsKey(field.Name)
                ? existingTypes[field.Name]
                : LeaderboardType.None;

            leaderboards.Add(new LeaderboardEntry{
                name = field.Name,
                type = type
            });
            Debug.Log($"[LeaderboardSettings] Added leaderboard: {field.Name} with type: {type}");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Debug.Log("[LeaderboardSettings] OnValidate called");
        RefreshLeaderboards();
    }
#endif
}
