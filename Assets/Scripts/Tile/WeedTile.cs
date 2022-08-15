using UnityEngine;
using Photon.Pun;
using NSMB.Utils;

[CreateAssetMenu(fileName = "WeedTile", menuName = "ScriptableObjects/Tiles/WeedTile", order = 2)]
public class WeedTile : BreakableBrickTile {
    public string resultTile;
    public override bool Interact(MonoBehaviour interacter, InteractionDirection direction, Vector3 worldLocation) {
        if (base.Interact(interacter, direction, worldLocation))
            return true;

        Vector3Int tileLocation = Utils.WorldToTilemapPosition(worldLocation);

        string spawnResult = "Weed";

        Bump(interacter, direction, worldLocation);

        object[] parametersBump = new object[]{tileLocation.x, tileLocation.y, direction == InteractionDirection.Down, resultTile, spawnResult};
        GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.BumpTile, parametersBump, ExitGames.Client.Photon.SendOptions.SendReliable);

        if (interacter is MonoBehaviourPun pun2)
            pun2.photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Powerup);
        return false;
    }
}
