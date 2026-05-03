
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Единый спрайт Астронавта (Эмодзи) с плавным наклоном при движении. v8.0 минимализм.
/// </summary>
public class AstroSpriteController : MonoBehaviour
{
    private Image _image;
    private PlayerCharacter _player;
    private RectTransform _rt;

    void Start()
    {
        _image  = GetComponent<Image>();
        _player = GetComponent<PlayerCharacter>();
        _rt = GetComponent<RectTransform>();

        // Загружаем эмодзи Астронавта
        Texture2D[] texs = Resources.LoadAll<Texture2D>("Emojis/Player");
        if (texs.Length > 0)
        {
            _image.sprite = Sprite.Create(texs[0], new Rect(0, 0, texs[0].width, texs[0].height), new Vector2(0.5f, 0.5f));
            _image.color = Color.white;
        }
    }

    void Update()
    {
        if (_player == null || _rt == null) return;
        
        // Наклоняем астронавта вверх/вниз в зависимости от скорости падения по оси Y
        float vy = _player.CurrentVelocityY;
        float targetAngle = Mathf.Clamp(vy * 0.15f, -30f, 30f);
        
        Quaternion currentRot = _rt.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
        _rt.rotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * 10f);
    }
}
