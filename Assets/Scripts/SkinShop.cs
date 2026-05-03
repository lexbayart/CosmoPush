using UnityEngine;
using UnityEngine.UI;

public class SkinShop : MonoBehaviour
{
    public Button buyButton;
    public Button equipButton;
    public Text coinsText;

    void Start()
    {
        UpdateCoins();
        if (buyButton != null) buyButton.onClick.AddListener(BuySkin);
        if (equipButton != null) equipButton.onClick.AddListener(EquipSkin);
    }

    void UpdateCoins()
    {
        //
        coinsText.text = "Coins: 0";
    }

    void BuySkin()
    {
        //
    }

    void EquipSkin()
    {
        //
    }
}
