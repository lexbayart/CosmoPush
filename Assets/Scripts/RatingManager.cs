using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


/// <summary>
/// Manages prompting the user to rate the game.
/// </summary>
public class RatingManager : MonoBehaviour
{
    public static RatingManager Instance;

    [Header("UI References")]
    public GameObject ratingPanel; // The panel to show when prompting for a rating
    public Button rateButton;      // Button to rate the game now
    public Button remindButton;    // Button to remind later
    public Button neverButton;     // Button to never show again

    [Header("Settings")]
    public int minGameSessionsBeforePrompt = 3; // How many game sessions before showing the prompt
    public string storeUrlAndroid = "market://details?id=com.yourcompany.cosmopush";
    public string storeUrlIOS = "https://apps.apple.com/app/id123456789";

    private int _gameSessionCount;
    private bool _hasRated;
    private bool _hasDeclined;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Load saved state
        _hasRated = PlayerPrefs.GetInt("HasRated", 0) == 1;
        _hasDeclined = PlayerPrefs.GetInt("HasDeclinedRating", 0) == 1;
        _gameSessionCount = PlayerPrefs.GetInt("GameSessionCount", 0);

        // Hide the rating panel if it exists
        if (ratingPanel != null)
        {
            ratingPanel.SetActive(false);
        }

        // Validate references
        if (rateButton == null || remindButton == null || neverButton == null)
        {
            Debug.LogWarning("[RatingManager] One or more button references are not assigned.");
        }
        else
        {
            // Assign button callbacks
            rateButton.onClick.AddListener(OnRateButtonClicked);
            remindButton.onClick.AddListener(OnRemindButtonClicked);
            neverButton.onClick.AddListener(OnNeverButtonClicked);
        }
    }

    /// <summary>
    /// Call this method to increment the game session count and potentially show the rating prompt.
    /// </summary>
    public void NotifyGameSessionEnded()
    {
        if (_hasRated || _hasDeclined)
            return;

        _gameSessionCount++;
        PlayerPrefs.SetInt("GameSessionCount", _gameSessionCount);
        PlayerPrefs.Save();

        if (_gameSessionCount >= minGameSessionsBeforePrompt)
        {
            ShowRatingPrompt();
        }
    }

    /// <summary>
    /// Shows the rating prompt UI.
    /// </summary>
    public void ShowRatingPrompt()
    {
        if (_hasRated || _hasDeclined)
            return;

        if (ratingPanel != null)
        {
            ratingPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[RatingManager] Rating panel reference is not assigned.");
        }
    }

    /// <summary>
    /// Called when the user clicks the Rate button.
    /// </summary>
    public void OnRateButtonClicked()
    {
        _hasRated = true;
        PlayerPrefs.SetInt("HasRated", 1);
        PlayerPrefs.Save();

        // Hide the panel
        if (ratingPanel != null)
        {
            ratingPanel.SetActive(false);
        }

        // Open the store page
        OpenStorePage();

        Debug.Log("[RatingManager] User rated the game.");
    }

    /// <summary>
    /// Called when the user clicks the Remind Later button.
    /// </summary>
    public void OnRemindButtonClicked()
    {
        // Reset the session count so we ask again after a few more sessions
        _gameSessionCount = 0;
        PlayerPrefs.SetInt("GameSessionCount", 0);
        PlayerPrefs.Save();

        // Hide the panel
        if (ratingPanel != null)
        {
            ratingPanel.SetActive(false);
        }

        Debug.Log("[RatingManager] User chose to be reminded later.");
    }

    /// <summary>
    /// Called when the user clicks the Never button.
    /// </summary>
    public void OnNeverButtonClicked()
    {
        _hasDeclined = true;
        PlayerPrefs.SetInt("HasDeclinedRating", 1);
        PlayerPrefs.Save();

        // Hide the panel
        if (ratingPanel != null)
        {
            ratingPanel.SetActive(false);
        }

        Debug.Log("[RatingManager] User chose to never be prompted again.");
    }

    /// <summary>
    /// Opens the appropriate store page based on the platform.
    /// </summary>
    private void OpenStorePage()
    {
        string url = "";
#if UNITY_ANDROID
        url = storeUrlAndroid;
#elif UNITY_IOS
        url = storeUrlIOS;
#else
        url = storeUrlAndroid; // fallback
#endif

        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning("[RatingManager] Store URL is not configured for this platform.");
        }
    }
}
