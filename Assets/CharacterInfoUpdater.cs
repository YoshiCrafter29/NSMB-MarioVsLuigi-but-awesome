using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoUpdater : MonoBehaviour
{
    public SkinnedMeshRenderer largeRenderer, smallRenderer;
    public Transform largeTransform, smallTransform;
    public MeshRenderer propellerRenderer;

    // Start is called before the first frame update
    void Start()
    {
        PlayerController controller = GetComponent<PlayerController>();
        Animator animator = GetComponent<Animator>();
        if (!controller || !animator) return;
        PlayerData playerData = controller.character;

        largeRenderer.sharedMesh = playerData.largeMesh;
        largeRenderer.materials = playerData.largeMaterials.ToArray();
        largeTransform.localScale = playerData.largeScale;
        smallRenderer.sharedMesh = playerData.smallMesh;
        smallRenderer.materials = playerData.smallMaterials.ToArray();
        smallTransform.localScale = playerData.smallScale;

        animator.Rebind();

        controller.AnimationController.shrinkBigModel = playerData.shrinkBigToMakeSmall;
        if (playerData.smallAvatar != null) controller.AnimationController.smallAvatar = playerData.smallAvatar;
        if (playerData.largeAvatar != null) controller.AnimationController.largeAvatar = playerData.largeAvatar;


        // PROPELLER!!!
        controller.AnimationController.propellerHelmet.transform.localPosition = playerData.propellerOffset;
        if (playerData.propellerMaterial != null) propellerRenderer.materials = new Material[] { playerData.propellerMaterial };
        if (playerData.propellerMaterial != null)
            propellerRenderer.materials = new Material[] { playerData.propellerMaterial };
        controller.AnimationController.propellerHelmet.transform.localScale = new Vector3(playerData.propellerScale, playerData.propellerScale, playerData.propellerScale);
        if (playerData.editPropellerRot)
            controller.AnimationController.propellerHelmet.transform.localRotation = Quaternion.Euler(playerData.propellerRot);

        // BLUE SHELL
        if (playerData.editShellRot)
            controller.AnimationController.blueShell.transform.localRotation = Quaternion.Euler(playerData.shellRotation);
        controller.AnimationController.blueShell.transform.localPosition = playerData.shellOffset;

        // SUITCASE
        controller.AnimationController.suitcase.transform.localPosition = playerData.handcasePosition;
        controller.AnimationController.suitcase.transform.localRotation = Quaternion.Euler(playerData.handcaseRotation);

        // SPECIAL BEHAVIOURS
        switch(playerData.specialBehaviour)
        {
            case SpecialBehaviour.STEVE:
                SteveTextureDownloader downloader = gameObject.AddComponent<SteveTextureDownloader>();
                downloader.skinnedMeshRenderer = largeRenderer;
                break;
            default:
                break;
        }
    }
}

