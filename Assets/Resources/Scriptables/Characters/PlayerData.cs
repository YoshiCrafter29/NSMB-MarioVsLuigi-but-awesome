using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 0)]
public class PlayerData : ScriptableObject {
    public string charName;

    [Header("Prefab settings")]
    public string prefab;
    public bool useLargeForSmallModel = false;

    [Header("Other")]
    public string soundFolder, uistring;
    public Sprite loadingSmallSprite, loadingBigSprite, readySprite;
    public RuntimeAnimatorController smallOverrides, largeOverrides;
}