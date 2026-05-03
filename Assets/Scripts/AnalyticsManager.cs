using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    private string logFilePath;

    void Awake()
    {
        // Ensure only one instance exists
        var instances = FindObjectsByType<AnalyticsManager>(FindObjectsSortMode.None);
        if (instances.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // Set up log file in persistent data path
        logFilePath = System.IO.Path.Combine(Application.persistentDataPath, "analytics.log");
        // Clear the log file on startup (or append)
        System.IO.File.WriteAllText(logFilePath, $"=== Analytics Log Started at {System.DateTime.Now} ===\n");
    }

    /// <summary>
    /// Logs an event with optional parameters.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="parameters">Optional dictionary of parameters.</param>
    public void LogEvent(string name, System.Collections.Generic.Dictionary<string, object> parameters = null)
    {
        try
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] EVENT: {name}";

            if (parameters != null && parameters.Count > 0)
            {
                var paramStrings = new System.Collections.Generic.List<string>();
                foreach (var kvp in parameters)
                {
                    paramStrings.Add($"{kvp.Key}={kvp.Value}");
                }
                logLine += " | " + string.Join(", ", paramStrings);
            }

            logLine += "\n";
            System.IO.File.AppendAllText(logFilePath, logLine);

            // Also log to Unity console for immediate feedback
            Debug.Log($"[Analytics] {logLine.Trim()}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] Failed to log event: {e.Message}");
        }
    }

    /// <summary>
    /// Logs a screen view.
    /// </summary>
    /// <param name="screenName">Name of the screen.</param>
    public void LogScreenView(string screenName)
    {
        LogEvent("screen_view", new System.Collections.Generic.Dictionary<string, object>
        {
            { "screen_name", screenName }
        });
    }

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="stackTrace">Optional stack trace.</param>
    public void LogError(string errorMessage, string stackTrace = null)
    {
        LogEvent("error", new System.Collections.Generic.Dictionary<string, object>
        {
            { "error_message", errorMessage },
            { "stack_trace", stackTrace ?? "" }
        });
    }

    /// <summary>
    /// Logs a custom metric.
    /// </summary>
    /// <param name="metricName">Name of the metric.</param>
    /// <param name="value">Numeric value.</param>
    public void LogMetric(string metricName, float value)
    {
        LogEvent("metric", new System.Collections.Generic.Dictionary<string, object>
        {
            { "metric_name", metricName },
            { "metric_value", value }
        });
    }
}
