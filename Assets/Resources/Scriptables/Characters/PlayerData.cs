using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 0)]
public class PlayerData : ScriptableObject {

    // can't stack them up on first line or else inspector goes nuts
    [Header("== General Character Settings ==")]
    public string characterName;
    public string uistring;
    [SerializeField]
    public SpecialBehaviour specialBehaviour = SpecialBehaviour.NONE;
    public Sprite loadingSmallSprite, loadingBigSprite, readySprite;


    [Header("== Voices Settings ==")]
    public string soundFolder;


    /**
     * TODO!!!
     * [Header("== Stats Settings ==")]
     */


    [Header("== Model Settings ==")]
    public Mesh smallMesh;
    public List<Material> smallMaterials;
    public Vector3 smallScale = new Vector3(0.01f, 0.01f, 0.01f);
    public Mesh largeMesh;
    public List<Material> largeMaterials;
    public Vector3 largeScale = new Vector3(0.018f, 0.018f, 0.018f);
    public Vector3 meshLocalPosition = new Vector3(0f, -0.02f, 0f);


    [Header("== Animation Settings ==")]
    public RuntimeAnimatorController smallOverrides;
    public RuntimeAnimatorController largeOverrides;
    public Avatar smallAvatar, largeAvatar;
    public bool shrinkBigToMakeSmall = false;

    [Header("Propeller Settings")]
    public Vector3 propellerOffset;
    public bool editPropellerRot = false;
    public Vector3 propellerRot;
    public Material propellerMaterial;
    public float propellerScale = 0.015f;

    [Header("Shell Settings")]
    public Vector3 shellOffset;
    public bool editShellRot = false;
    public Vector3 shellRotation;

    [Header("Handcase")]
    public Vector3 handcasePosition = new Vector3(0.0005f, -0.0069f, -0.0007f);
    public Vector3 handcaseRotation = new Vector3(7f, 2.42f, 0f);
}

public enum SpecialBehaviour : short
{
    NONE = 0,
    STEVE = 1,
    MCQUEEN = 2
}