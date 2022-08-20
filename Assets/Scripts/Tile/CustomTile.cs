using UnityEngine;
using Photon.Pun;
using NSMB.Utils;

[CreateAssetMenu(fileName = "CustomTile", menuName = "ScriptableObjects/Tiles/CustomTile", order = 2)]
public class CustomTile : BreakableBrickTile {
    public string resultTile;
    public string spawnResult = "";
    public override bool Interact(MonoBehaviour interacter, InteractionDirection direction, Vector3 worldLocation) {
        if (base.Interact(interacter, direction, worldLocation))
            return true;

        Vector3Int tileLocation = Utils.WorldToTilemapPosition(worldLocation);

        Powerup[] powerups = Utils.GetPowerups();


        Utils.GetCustomProperty(Enums.NetRoomProperties.ModdedPowerups, out bool modded);
        Utils.GetCustomProperty(Enums.NetRoomProperties.NewPowerups, out bool custom);

        bool canSpawn = true;
        foreach (Powerup p in powerups)
        {
            if (p.prefab == spawnResult)
            {
                if (!custom && p.custom) canSpawn = false;
                if (!modded && p.state >= Enums.PowerupState.Suit) canSpawn = false;
                break;
            }
        }

        if (canSpawn)
        {
            Bump(interacter, direction, worldLocation);

            object[] parametersBump = new object[] { tileLocation.x, tileLocation.y, direction == InteractionDirection.Down, resultTile, spawnResult, new Vector2(0f, 0.5f) };
            GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.BumpTile, parametersBump, ExitGames.Client.Photon.SendOptions.SendReliable);

            if (interacter is MonoBehaviourPun pun2)
                pun2.photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Powerup);
        } else
        {
            Break(interacter, worldLocation, Enums.Sounds.World_Block_Break);
        }
        return !canSpawn;
    }
}
