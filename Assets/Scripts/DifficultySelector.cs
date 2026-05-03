using UnityEngine;
using UnityEngine.UI;

public class DifficultySelector : MonoBehaviour
{
    public Dropdown difficultyDropdown;

    void Start()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>{ "Easy", "Normal", "Hard" });
            difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 1);
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }

    void OnDifficultyChanged(int index)
    {
        PlayerPrefs.SetInt("Difficulty", index);
        PlayerPrefs.Save();
    }
}
