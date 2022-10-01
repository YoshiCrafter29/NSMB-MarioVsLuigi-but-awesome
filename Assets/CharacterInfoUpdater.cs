using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoUpdater : MonoBehaviour
{
    public SkinnedMeshRenderer largeRenderer, smallRenderer;

    // Start is called before the first frame update
    void Start()
    {
        PlayerController controller = GetComponent<PlayerController>();
        Animator animator = GetComponent<Animator>();
        if (!controller || !animator) return;
        PlayerData playerData = controller.character;

        largeRenderer.sharedMesh = playerData.largeMesh;
        largeRenderer.materials = playerData.largeMaterials.ToArray();
        smallRenderer.sharedMesh = playerData.smallMesh;
        smallRenderer.materials = playerData.smallMaterials.ToArray();

        animator.Rebind();

        // PROPELLER!!!
        controller.AnimationController.propellerHelmet.transform.localPosition = playerData.propellerOffset;
        if (playerData.smallAvatar != null) controller.AnimationController.smallAvatar = playerData.smallAvatar;
        if (playerData.largeAvatar != null) controller.AnimationController.largeAvatar = playerData.largeAvatar;
        if (playerData.propellerMaterial != null)
                controller.AnimationController.propellerHelmet.GetComponent<SkinnedMeshRenderer>().materials = new Material[] { playerData.propellerMaterial };
    }
}

