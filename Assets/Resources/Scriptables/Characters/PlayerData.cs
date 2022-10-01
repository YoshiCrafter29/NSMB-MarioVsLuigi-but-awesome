using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 0)]
public class PlayerData : ScriptableObject {

    // can't stack them up on first line or else inspector goes nuts
    [Header("== UI Settings ==")]
    public string characterName;
    public string uistring;
    public Sprite loadingSmallSprite, loadingBigSprite, readySprite;


    [Header("== Voices Settings ==")]
    public string soundFolder;


    [Header("== Model Settings ==")]
    public Mesh smallMesh;
    public List<Material> smallMaterials;
    public Mesh largeMesh;
    public List<Material> largeMaterials;
    public Vector3 meshLocalPosition;


    [Header("== Animation Settings ==")]
    public RuntimeAnimatorController smallOverrides;
    public RuntimeAnimatorController largeOverrides;
    public Avatar smallAvatar, largeAvatar;
    public bool isLuigiModel, shrinkBigToMakeSmall = false;

    [Header("Propeller Settings")]
    public Vector3 propellerOffset;
    public Material propellerMaterial;

    [Header("Shell Settings")]
    public Vector3 shellOffset;

    [Header("Handcase")]
    public Vector3 handcasePosition;
    public Vector3 handcaseRotation;


    /*
    [Header("DEEZ NUTS!!")]
    public bool deezNuts = false;
    */
}