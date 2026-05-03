using UnityEngine;

public class StreakManager : MonoBehaviour
{
    private int currentStreak = 0;
    public int CurrentStreak => currentStreak;

    public void AddStreak()
    {
        currentStreak++;
    }

    public void ResetStreak()
    {
        currentStreak = 0;
    }
}
