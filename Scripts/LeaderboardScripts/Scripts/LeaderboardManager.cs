using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;
using Dan.Main;
using Dan.Models;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LeaderbordManager
{
    /// <summary>
    /// Configuration class for leaderboard settings containing the name, public key and type.
    /// </summary>
    [System.Serializable]
    public class LeaderboardConfig
    {
        public string name;
        public string publicKey;
        public LeaderboardType type;
    }

    /// <summary>
    /// Defines the available types of leaderboards in the system.
    /// </summary>
    public enum LeaderboardType
    {
        None,
        Basic,
        Extra
    }

    /// <summary>
    /// Defines the header types available for leaderboard displays.
    /// </summary>
    public enum HeaderType
    {
        Basic,
        Extra,
    }

    /// <summary>
    /// Defines the score types that can be submitted to the leaderboard.
    /// </summary>
    public enum ScoreType
    {
        Integer,
        Float
    }

    /// <summary>
    /// Custom UnityEvent for handling score submissions with type, username, score and extra data.
    /// </summary>
    [System.Serializable]
    public class ScoreSubmissionEvent : UnityEvent<LeaderboardType, string, int, string> { }

    public class ReadOnlyAttribute : PropertyAttribute { }

    /// <summary>
    /// Main manager class for handling leaderboard operations and UI interactions.
    /// </summary>
    /// <remarks>
    /// This class manages:
    /// <list type="bullet">
    /// <item><description>Leaderboard initialization and configuration</description></item>
    /// <item><description>Score submission and validation</description></item>
    /// <item><description>UI updates and animations</description></item>
    /// <item><description>Auto-refresh functionality</description></item>
    /// </list>
    /// </remarks>
    public class LeaderboardManager : MonoBehaviour
    {
        // #region Singleton
        //
        // /// <summary>
        // /// Singleton instance of the LeaderboardManager.
        // /// </summary>
        // public static LeaderboardManager Instance { get; private set; }
        //
        // #endregion

        #region Constants

        private static readonly int RQ_SUCCESS = Animator.StringToHash("rq_succ");
        private static readonly int RQ_FAIL = Animator.StringToHash("rq_fail");
        private static readonly int RQ_WARN = Animator.StringToHash("rq_warn");
        private const string ERROR_NO_NAME = "<i>No name entered!</i>";
        private const string UPLOADING_TEXT = "<i>Uploading score...</i>";
        private const string LOADING_TEXT = "<i>Loading scores...</i>";

        #endregion

        #region Core Settings

        [SerializeField] private LeaderboardSettings settings;
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private Transform leaderboardHeader;
        [Range(3, 10)] [SerializeField] private int refreshInterval;

        [Tooltip("How many units to move the input field after submission")] [SerializeField]
        private int moveInputAfterUnits = -400;

        #endregion

        #region State Variables

        [Header("Read only variables")] [SerializeField, ReadOnly]
        private LeaderboardType currentLeaderboardType;

        [SerializeField, ReadOnly] private int globalScore;
        [SerializeField, ReadOnly] private bool isApiOperationInProgress;
        [SerializeField, ReadOnly] private bool isWaitingForConfirmation;
        [SerializeField, ReadOnly] private string pendingUsername;
        private Entry? playerEntry;
        private string leaderboardExtra;

        #endregion

        #region UI References

        [Header("UI References")] [SerializeField]
        private TMP_InputField inputName;

        [SerializeField] private TextMeshProUGUI highestScore;
        [SerializeField] private Animator msgAnimator;
        [SerializeField] private Button setScoreButton;
        [SerializeField] private TextMeshProUGUI registeredUsernameText;

        #endregion

        #region Prefabs

        [Header("Header Elements")] [SerializeField]
        private GameObject basicHeaderPrefab;

        [SerializeField] private GameObject extraHeaderPrefab;

        [Header("Row Elements")] [SerializeField]
        private ScoreRowElement basicRowElement;

        [SerializeField] private ExtraRowElement extraRowElement;

        #endregion

        #region Internal References

        private Dictionary<string, LeaderboardReference> leaderboardRefs =
            new Dictionary<string, LeaderboardReference>();

        private Dictionary<LeaderboardType, Action<Entry[]>> entryHandlers;
        private Dictionary<LeaderboardType, HeaderType> headerTypes;
        private ScoreSubmissionEvent submitScoreEvent;

        #endregion

        #region Coroutines

        private Coroutine autoRefreshCoroutine;
        private Coroutine loadingTimerCoroutine;

        #endregion

        #region Core Methods

            /// <summary>
            /// Initializes the leaderboard system and sets up all required components.
            /// </summary>
            /// <remarks>
            /// Performs the following initialization steps:
            /// <list type="number">
            /// <item><description>Sets up the singleton instance</description></item>
            /// <item><description>Initializes score submission events</description></item>
            /// <item><description>Sets up row handlers and header mappings</description></item>
            /// <item><description>Prepares the leaderboard references</description></item>
            /// </list>
            /// </remarks>
            private void Awake()
            {

                // if (Instance != null && Instance != this)
                // {
                //     Destroy(gameObject);
                //     return;
                // }
                //
                // Instance = this;
                // DontDestroyOnLoad(gameObject);

                // Initialize score submission event
                if (submitScoreEvent == null)
                    submitScoreEvent = new ScoreSubmissionEvent();

                // Auto adds the HandleScoreSubmission method to the event
                submitScoreEvent.AddListener(HandleScoreSubmission);

                InitializeRowHandlers();
                InitializeHeaderMap();
                InitializeLeaderboardRefs();

                currentLeaderboardType = LeaderboardType.None;

                // Delete all children of the leaderboard container
                ClearLeaderboard();
                msgAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            }

            /// <summary>
            /// Initializes references to all available leaderboards in the system.
            /// </summary>
            /// <remarks>
            /// Uses reflection to find and store references to all leaderboard instances.
            /// Automatically refreshes settings before initialization.
            /// </remarks>
            private void InitializeLeaderboardRefs()
            {
                Debug.Log("[LeaderboardManager] Initializing leaderboard references...");

                // First refresh settings to ensure we have latest leaderboards
                settings.RefreshLeaderboards();

                // Clear existing refs
                leaderboardRefs.Clear();

                // Get all leaderboard fields from generated class
                var fields = typeof(Dan.Main.Leaderboards).GetFields(BindingFlags.Public | BindingFlags.Static);

                foreach (var field in fields)
                {
                    if (field.GetValue(null) is Dan.Main.LeaderboardReference reference)
                    {
                        leaderboardRefs[field.Name] = reference;
                        Debug.Log($"[LeaderboardManager] Added reference for leaderboard: {field.Name}");
                    }
                }

                Debug.Log($"[LeaderboardManager] Initialized {leaderboardRefs.Count} leaderboard references");
            }

            /// <summary>
            /// Initializes handlers for each leaderboard type.
            /// </summary>
            /// <remarks>
            /// Configures the mapping between LeaderboardType and their corresponding entry handlers.
            /// This enables GetLeaderboardEntries to process entries based on leaderboard type.
            /// </remarks>
            private void InitializeRowHandlers()
            {
                entryHandlers = new Dictionary<LeaderboardType, Action<Entry[]>>
                {
                    { LeaderboardType.None, (_) => Debug.LogError("Leaderboard type not set!") },
                    { LeaderboardType.Basic, HandleBasicEntries },
                    { LeaderboardType.Extra, HandleExtraEntries }
                };
            }

            /// <summary>
            /// Initializes the mapping between leaderboard types and their corresponding header prefabs.
            /// </summary>
            /// <remarks>
            /// Creates a dictionary that maps each LeaderboardType to its appropriate HeaderType,
            /// ensuring correct header visualization for each leaderboard variant.
            /// </remarks>
            private void InitializeHeaderMap()
            {
                headerTypes = new Dictionary<LeaderboardType, HeaderType>
                {
                    { LeaderboardType.None, HeaderType.Basic },
                    { LeaderboardType.Basic, HeaderType.Basic },
                    { LeaderboardType.Extra, HeaderType.Extra }
                };
            }

            /// <summary>
            /// Initializes a leaderboard with the specified name, score, and optional extra data.
            /// </summary>
            /// <param name="leaderboardName">The name of the leaderboard to initialize</param>
            /// <param name="score">The initial score to set</param>
            /// <param name="extra">Optional extra data for the leaderboard</param>
            /// <remarks>
            /// This method performs the following steps:
            /// <list type="bullet">
            /// <item><description>Validates the leaderboard name exists in references</description></item>
            /// <item><description>Checks for valid configuration</description></item>
            /// <item><description>Sets up the current leaderboard type and score</description></item>
            /// <item><description>Spawns appropriate header and retrieves entries</description></item>
            /// </list>
            /// </remarks>
            public void InitializeLeaderboard(string leaderboardName, int score, string extra = "")
            {
                if (!leaderboardRefs.ContainsKey(leaderboardName))
                {
                    Debug.LogError($"Leaderboard {leaderboardName} not found!");
                    return;
                }

                var foundLeaderboard = settings.leaderboards.Find(x => x.name == leaderboardName);
                if (foundLeaderboard == null)
                {
                    Debug.LogError($"Leaderboard {leaderboardName} configuration not found!");
                    return;
                }

                currentLeaderboardType = foundLeaderboard.type;
                globalScore = score;
                leaderboardExtra = extra;

                SetCurrentLeaderboard(foundLeaderboard.type);
                SpawnHeader(headerTypes[foundLeaderboard.type]);
                GetLeaderboardEntries(foundLeaderboard.type);
            }

            /// <summary>
            /// Starts the auto-refresh coroutine for the leaderboard.
            /// </summary>
            private void StartLeaderBoardRefresh()
            {
                if (autoRefreshCoroutine != null) return;
                autoRefreshCoroutine = StartCoroutine(AutoRefreshLeaderboard());
            }


            /// <summary>
            /// Spawns a header on <see cref="leaderboardHeader"/> based on the specified <see cref="HeaderType"/>.
            /// </summary>
            /// <param name="headerType">The type of header to spawn</param>
            /// <remarks>
            /// The method handles the following <see cref="HeaderType"/> cases by default:
            /// <list type="bullet">
            /// <item><description><see cref="HeaderType.Basic"/>: Spawns the "basic" header.</description></item>
            /// <item><description><see cref="HeaderType.Extra"/>: Spawns the "extra" header.</description></item>
            /// </list>
            /// </remarks>
            private void SpawnHeader(HeaderType headerType)
            {
                // Only destroy existing headers, not the entire leaderboard

                const string newHeaderTag = "LBD_Header";
                var existingHeaders = leaderboardHeader.GetComponentsInChildren<Transform>()
                    .Where(t => t.CompareTag(newHeaderTag))
                    .ToList();

                foreach (Transform header in existingHeaders)
                {
                    if (header && header != leaderboardHeader) // Don't destroy the container
                        Destroy(header.gameObject);
                }

                // Spawn new header
                GameObject headerPrefab = headerType switch
                {
                    HeaderType.Extra => extraHeaderPrefab,
                    HeaderType.Basic => basicHeaderPrefab,
                    _ => basicHeaderPrefab
                };

                GameObject newHeader = Instantiate(headerPrefab, leaderboardHeader);
                newHeader.tag = newHeaderTag; // Mark as header for future identification
                newHeader.transform.SetAsFirstSibling();
            }

            #endregion

            #region Leaderboard Management

            /// <summary>
            /// Sets the current type of leaderboard to be displayed and managed.
            /// </summary>
            /// <param name="type">The leaderboard type to set as current</param>
            private void SetCurrentLeaderboard(LeaderboardType type)
            {
                currentLeaderboardType = type;
            }

            /// <summary>
            /// Retrieves entries for the specified leaderboard type.
            /// </summary>
            /// <param name="type">The type of leaderboard to fetch entries from</param>
            /// <exception cref="InvalidOperationException">Thrown when type is LeaderboardType.None</exception>
            /// <remarks>
            /// This method:
            /// <list type="bullet">
            /// <item><description>Validates the leaderboard type</description></item>
            /// <item><description>Updates the loading state</description></item>
            /// <item><description>Fetches entries from the server</description></item>
            /// <item><description>Handles success and failure cases</description></item>
            /// </list>
            /// </remarks>
            private void GetLeaderboardEntries(LeaderboardType type)
            {
                if (type == LeaderboardType.None)
                {
                    Debug.LogError("Cannot get entries - Leaderboard type not set");
                    return;
                }

                highestScore.text = LOADING_TEXT;

                isApiOperationInProgress = true;
                if (loadingTimerCoroutine != null) StopCoroutine(loadingTimerCoroutine);
                loadingTimerCoroutine = StartCoroutine(UpdateLoadingText());

                LeaderboardReference leaderboard = GetLeaderboardReference(type);
                LeaderboardCreator.Ping(online =>
                    {
                        if (!online)
                        {
                            isApiOperationInProgress = false;
                            OnEntriesLoadFailed("Server down, try again later :(");
                        }
                    }
                );

                leaderboard.GetEntries(
                    entries =>
                    {
                        isApiOperationInProgress = false;
                        entryHandlers[type](entries);
                    },
                    error =>
                    {
                        isApiOperationInProgress = false;
                        OnEntriesLoadFailed($"Error loading scores, {error}");
                    });
            }

            /// <summary>
            /// Gets the reference to the specified leaderboard type.
            /// </summary>
            /// <param name="type">The type of leaderboard to get the reference for</param>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException">If the leaderboard type is <see cref="LeaderboardType.None"/></exception>
            /// <exception cref="ArgumentException">If the leaderboard type is unknown</exception>
            private LeaderboardReference GetLeaderboardReference(LeaderboardType type)
            {
                var setLeaderboard = settings.leaderboards.Find(x => x.type == type);
                if (setLeaderboard == null)
                    throw new InvalidOperationException($"No leaderboard configured for type {type}");

                return leaderboardRefs[setLeaderboard.name];
            }

            /// <summary>
            /// Destroys all child elements of the <see cref="leaderboardContainer"/>.
            /// </summary>
            private void ClearLeaderboard()
            {
                SetRegisteredUsername(null);
                foreach (Transform child in leaderboardContainer)
                {
                    Destroy(child.gameObject);
                }
            }

        #endregion


        #region Entry Handlers

            /// <summary>
            /// Handles the entries for the basic leaderboard type.
            /// </summary>
            /// <param name="entries">Array of leaderboard entries to handle.</param>
            private void HandleBasicEntries(Entry[] entries)
            {
                ClearLeaderboard();
                if (entries.Length == 0)
                {
                    highestScore.text = "No records found";
                    return;
                }

                foreach (var entry in entries)
                {
                    var row = Instantiate(basicRowElement, leaderboardContainer);
                    string dateStr = DateTimeOffset.FromUnixTimeSeconds((long)entry.Date)
                        .UtcDateTime.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture);

                    if (entry.IsMine())
                    {
                        playerEntry = entry;
                        row.SetWithMine(entry.Username, entry.Score, dateStr);
                        SetRegisteredUsername(entry.Username);
                    }
                    else
                    {
                        row.Set(entry.Username, entry.Score, dateStr);
                    }
                }

                highestScore.text = $"Record by {entries[0].Username}: {entries[0].Score} points";
            }

            private void HandleExtraEntries(Entry[] entries)
            {
                ClearLeaderboard();
                if (entries.Length == 0)
                {
                    highestScore.text = "No records found";
                    return;
                }

                foreach (var entry in entries)
                {
                    var row = Instantiate(extraRowElement, leaderboardContainer);
                    string dateStr = DateTimeOffset.FromUnixTimeSeconds((long)entry.Date)
                        .UtcDateTime.ToString("HH:mm dd/MM", CultureInfo.InvariantCulture);

                    if (entry.IsMine())
                    {
                        playerEntry = entry;
                        row.SetWithMine(entry.Username, entry.Score, dateStr, entry.Extra);
                        SetRegisteredUsername(entry.Username);
                        continue;
                    }

                    row.Set(entry.Username, entry.Score, dateStr, entry.Extra);
                }

                highestScore.text =
                    $"Record by {entries[0].Username}: {entries[0].Score} points. Extra: {entries[0].Extra}";
            }

            /// <summary>
            /// Updates the registered username display in the UI.
            /// </summary>
            /// <param name="username">The username to display, or null to show unregistered state</param>
            /// <remarks>
            /// When username is null, displays "User is not registered".
            /// The username display is automatically cleared when the leaderboard is cleared.
            /// </remarks>
            void SetRegisteredUsername(string username)
            {
                if (username == null)
                {
                    registeredUsernameText.text = $"User is not registered";
                    return;
                }

                registeredUsernameText.text = $"User: {username}";
            }

        #endregion

        #region Score Submission
            /// <summary>
            /// Submits the current score using the username from the input field.
            /// </summary>
            /// <remarks>
            /// This method:
            /// <list type="bullet">
            /// <item><description>Validates the input username</description></item>
            /// <item><description>Handles confirmation for overwriting existing scores</description></item>
            /// <item><description>Manages the submission UI state</description></item>
            /// <item><description>Triggers the score submission event</description></item>
            /// </list>
            /// </remarks>
            public void SubmitScore()
            {
                // Stop auto-refresh
                if (autoRefreshCoroutine != null)
                {
                    StopCoroutine(autoRefreshCoroutine);
                    autoRefreshCoroutine = null;
                }

                if (currentLeaderboardType == LeaderboardType.None)
                {
                    Debug.LogError("Cannot submit score - Leaderboard type not set!");
                    return;
                }

                // Clean and validate input
                string username = inputName.text.Trim();
                if (string.IsNullOrEmpty(username) || username == ERROR_NO_NAME || username == UPLOADING_TEXT)
                {
                    Debug.LogWarning("Username is empty or contains error message!");
                    inputName.text = "";
                    inputName.placeholder.GetComponent<TextMeshProUGUI>().text = ERROR_NO_NAME;
                    return;
                }

                // Handle override confirmation flow
                if (playerEntry?.IsMine() == true)
                {
                    if (!isWaitingForConfirmation)
                    {
                        // First click - enter confirmation state
                        isWaitingForConfirmation = true;
                        pendingUsername = username;
                        // inputName.text = "";
                        inputName.placeholder.GetComponent<TextMeshProUGUI>().text = "WARNING_OVERRIDE";
                        inputName.interactable = false;
                        msgAnimator.SetTrigger(RQ_WARN);
                        return;
                    }
                    else
                    {
                        // Second click - submit with stored username
                        isWaitingForConfirmation = false;
                        username = pendingUsername;
                        inputName.interactable = false;
                        setScoreButton.interactable = false;
                    }
                }

                // Prepare submission data
                string extra = leaderboardExtra;

                // Submit score
                submitScoreEvent.Invoke(currentLeaderboardType, username, globalScore, extra);
            }

            /// <summary>
            /// Handles the score submission event by uploading the score to the server.
            /// </summary>
            private void HandleScoreSubmission(LeaderboardType type, string username, int score, string extra)
            {
                UploadScore(username, score, extra);
            }

            private void UploadScore(string username, int score, string extra = "")
            {
                // First check if we have a previous entry
                if (playerEntry?.IsMine() == true)
                {
                    LeaderboardReference leaderboard = GetLeaderboardReference(currentLeaderboardType);

                    // Delete existing entry first
                    leaderboard.DeleteEntry(
                        (deleteSuccess) =>
                        {
                            if (deleteSuccess)
                            {
                                // After successful deletion, upload new entry
                                PerformUpload(leaderboard, username, score, extra);
                            }
                            else
                            {
                                Debug.LogError("<color=red>Failed to delete existing entry</color>");
                                OnUploadError();
                            }
                        },
                        (error) =>
                        {
                            Debug.LogError($"<color=red>Error deleting entry: {error}</color>");
                            OnUploadError();
                        }
                    );
                }
                else
                {
                    // No existing entry, just upload new one
                    LeaderboardReference leaderboard = GetLeaderboardReference(currentLeaderboardType);
                    PerformUpload(leaderboard, username, score, extra);
                }
            }

            private void PerformUpload(LeaderboardReference leaderboard, string username, int score, string extra)
            {
                inputName.interactable = false;
                inputName.placeholder.GetComponent<TextMeshProUGUI>().text = UPLOADING_TEXT;
                inputName.text = "";

                if (string.IsNullOrEmpty(extra))
                {
                    extra = " ";
                }

                leaderboard.UploadNewEntry(username, score, extra,
                    (checkConnection) =>
                    {
                        if (!checkConnection) return;
                        Debug.Log(
                            $"Score uploaded successfully to <color=green>{currentLeaderboardType} leaderboard</color>");
                        GetLeaderboardEntries(currentLeaderboardType);
                        RectTransform rt = inputName?.GetComponent<RectTransform>();
                        if (!rt)
                        {
                            Debug.LogError("RectTransform is missing on inputName!");
                            return;
                        }

                        StartCoroutine(UIAnimations.Instance.AnimateUIElement(rt, true,
                            rt.anchoredPosition + new Vector2(0, moveInputAfterUnits), rt.anchoredPosition, 1f, null,
                            true));
                        msgAnimator.SetTrigger(RQ_SUCCESS);
                        inputName.interactable = false;
                        ResetWarningState();
                        // Add to end of method:
                        StartLeaderBoardRefresh(); // Resume auto-refresh
                    },
                    (error) => OnUploadError()
                );
            }

            private void OnUploadError()
            {
                inputName.interactable = true;
                inputName.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter your nickname again";
                msgAnimator.SetTrigger(RQ_FAIL);
                setScoreButton.interactable = true;
                ResetWarningState();
            }

            /// <summary>
            /// Writes an error to the <see cref="highestScore"/> if the entries load fails.
            /// </summary>
            /// <param name="error">Text to write</param>
            private void OnEntriesLoadFailed(string error)
            {
                highestScore.text = error;
            }

            private void ResetWarningState()
            {
                isWaitingForConfirmation = false;
                pendingUsername = null;
            }

        #endregion

        #region Corutines

        private IEnumerator AutoRefreshLeaderboard()
        {
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(refreshInterval);
            while (true)
            {
                float startTime = Time.unscaledTime;
                Debug.Log($"Starting LeaderboardManager auto refresh...");
                GetLeaderboardEntries(currentLeaderboardType);
                yield return waitTime;
                float duration = Time.unscaledTime - startTime;
                Debug.Log($"LeaderboardManager refresh took {duration:F2} seconds");
            }
        }

        private IEnumerator UpdateLoadingText()
        {
            string baseText = LOADING_TEXT;
            float startTime = Time.unscaledTime;
            WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.1f);
            Debug.Log("Starting loading text update...");

            while (isApiOperationInProgress)
            {
                float elapsed = Time.unscaledTime - startTime;
                highestScore.text = $"{baseText} ({elapsed:F1}s)";
                yield return waitTime;
            }

            float duration = Time.unscaledTime - startTime;
            Debug.Log($"Loading text update completed in {duration:F2} seconds");
        }

        #endregion
    }
}
