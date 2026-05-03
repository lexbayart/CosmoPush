
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OxygenIndicator : MonoBehaviour
{
    public Image[] circles; // Замена на скачанные картинки-сердечки (v31.0)
    public Color normalColor = Color.white; // Белый, чтобы не портить цвет исходной картинки
    public Color lostColor = new Color(0, 0, 0, 0); 

    private int _currentOx = 5;
    private RhythmManager _rhythm;

    void Start()
    {
        _rhythm = GameObject.FindFirstObjectByType<RhythmManager>();
        if (_rhythm != null) _rhythm.OnBeat += HandleBeat;
    }

    void OnDestroy()
    {
        if (_rhythm != null) _rhythm.OnBeat -= HandleBeat;
    }

    private void HandleBeat(bool isDown)
    {
        StopCoroutine("PulseCoroutine");
        StartCoroutine(PulseCoroutine());
    }

    private IEnumerator PulseCoroutine()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        
        // Если осталась 1 жизнь, пульсируем сильнее (v1.2)
        float pulseMult = (_currentOx == 1) ? 1.5f : 1.2f;

        while (elapsed < duration)
        {
            float s = Mathf.Lerp(1f, pulseMult, elapsed / duration);
            ApplyScale(s);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            float s = Mathf.Lerp(pulseMult, 1f, elapsed / duration);
            ApplyScale(s);
            elapsed += Time.deltaTime;
            yield return null;
        }
        ApplyScale(1f);
    }

    private void ApplyScale(float s)
    {
        for (int i = 0; i < _currentOx; i++)
        {
            if (circles[i] != null) circles[i].rectTransform.localScale = new Vector3(s, s, 1f);
        }
    }

    public void UpdateOxygen(int ox)
    {
        _currentOx = ox;
        if (circles == null || circles.Length == 0) return;

        for (int i = 0; i < circles.Length; i++)
        {
            if (i < ox)
                circles[i].color = normalColor;
            else
            {
                circles[i].color = lostColor;
                circles[i].rectTransform.localScale = Vector3.one; // Сброс масштаба для пустых
            }
        }
    }
}
