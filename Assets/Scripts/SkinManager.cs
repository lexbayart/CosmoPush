using UnityEngine;
using UnityEngine.UI;

public class SkinManager : MonoBehaviour
{
    // Manages equipped skins
    private string equippedSkin = "default";

    void Start()
    {
        // Load default skin on start
        ApplySkin(equippedSkin);
    }

    public void ApplySkin(string skinName)
    {
        equippedSkin = skinName;
        // Attempt to apply skin to player character
        var player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            var sprite = Resources.Load<Sprite>($"Skins/{skinName}");
            if (sprite != null)
            {
                var image = player.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                }
                else
                {
                    // Try to find child image
                    var childImage = player.GetComponentInChildren<Image>();
                    if (childImage != null)
                    {
                        childImage.sprite = sprite;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[SkinManager] Skin sprite not found: Skins/{skinName}");
            }
        }
        else
        {
            Debug.LogWarning("[SkinManager] PlayerCharacter not found to apply skin.");
        }
    }
}
