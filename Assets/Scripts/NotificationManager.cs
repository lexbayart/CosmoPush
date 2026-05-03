using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("UI References")]
    public GameObject notificationPrefab; // Prefab with Text and optional background
    public Transform notificationContainer; // Where to spawn notifications (e.g., a Vertical Layout Group)
    public float defaultDuration = 3f;

    private Queue<NotificationData> _queue = new Queue<NotificationData>();
    private Coroutine _displayCoroutine;

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
        // Validate references
        if (notificationPrefab == null)
        {
            Debug.LogError("[NotificationManager] notificationPrefab is not assigned!");
        }
        if (notificationContainer == null)
        {
            Debug.LogWarning("[NotificationManager] notificationContainer is not assigned; notifications will not be parented.");
        }
    }

    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="duration">How long to show the notification (in seconds). Use defaultDuration if 0.</param>
    public void ShowNotification(string message, float duration = 0f)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("[NotificationManager] Attempted to show empty notification.");
            return;
        }

        float dur = duration <= 0f ? defaultDuration : duration;
        _queue.Enqueue(new NotificationData(message, dur));

        // Start the display coroutine if not already running
        if (_displayCoroutine == null)
        {
            _displayCoroutine = StartCoroutine(DisplayNotifications());
        }
    }

    private IEnumerator DisplayNotifications()
    {
        while (_queue.Count > 0)
        {
            var data = _queue.Dequeue();
            GameObject notificationObj = null;

            try
            {
                if (notificationPrefab != null)
                {
                    notificationObj = Instantiate(notificationPrefab, notificationContainer ? notificationContainer : transform);
                    // Find the Text component (assume it's on the prefab or a child)
                    var textComp = notificationObj.GetComponentInChildren<Text>();
                    if (textComp != null)
                    {
                        textComp.text = data.Message;
                    }
                    else
                    {
                        Debug.LogWarning("[NotificationManager] No Text component found in notification prefab.");
                    }
                }
                else
                {
                    Debug.LogError("[NotificationManager] notificationPrefab is missing; cannot show notification.");
                    yield break;
                }

                // Wait for the duration
                float elapsed = 0f;
                while (elapsed < data.Duration)
                {
                    elapsed += Time.unscaledDeltaTime; // Use unscaled time so notifications show during pauses
                    yield return null;
                }
            }
            finally
            {
                // Clean up the notification object
                if (notificationObj != null)
                {
                    Destroy(notificationObj);
                }
            }
        }

        _displayCoroutine = null;
    }

    private class NotificationData
    {
        public string Message;
        public float Duration;

        public NotificationData(string message, float duration)
        {
            Message = message;
            Duration = duration;
        }
    }
}
