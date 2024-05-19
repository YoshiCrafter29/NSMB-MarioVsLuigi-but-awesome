using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 0)]
public class PlayerData : ScriptableObject {
    public string charName;
    [Header("Prefab settings")]
    public bool useLuigiBase;
    public bool shrinkModelForSmall = false;

    public Mesh largeMarioModel;
    public Material[] largeMarioMaterials;
    public Mesh smallMarioModel;
    public Material[] smallMarioMaterials;

    [Header("Other")]
    public string soundFolder, uistring;
    public Sprite loadingSmallSprite, loadingBigSprite, readySprite;
    public RuntimeAnimatorController smallOverrides, largeOverrides;
}