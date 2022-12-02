using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapItem : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Image image;
    public MainMenuManager mainMenuManager;
    
    public void SetMap(string mapName, Sprite mapIcon = null)
    {
        text.text = mapName;
        if (mapIcon != null)
            image.sprite = mapIcon;
    }

    public void OnPress()
    {
        mainMenuManager.SetLevelIndex(transform.GetSiblingIndex() - 1);
        mainMenuManager.CloseMapSelectionMenu();
    }
}
