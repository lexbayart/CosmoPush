using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    void Start()
    {
        LoadSettings();
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggle);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggle);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolume);
    }

    void LoadSettings()
    {
        //
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt("SfxOn", 1) == 1;
        float musicVol = PlayerPrefs.GetFloat("MusicVol", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SfxVol", 0.5f);

        if (musicToggle != null) musicToggle.isOn = musicOn;
        if (sfxToggle != null) sfxToggle.isOn = sfxOn;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
    }

    void OnMusicToggle(bool on)
    {
        PlayerPrefs.SetInt("MusicOn", on ? 1 : 0);
        //
    }

    void OnSfxToggle(bool on)
    {
        PlayerPrefs.SetInt("SfxOn", on ? 1 : 0);
        //
    }

    void OnMusicVolume(float vol)
    {
        PlayerPrefs.SetFloat("MusicVol", vol);
        //
    }

    void OnSfxVolume(float vol)
    {
        PlayerPrefs.SetFloat("SfxVol", vol);
        //
    }
}
