using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GayTile", menuName = "ScriptableObjects/Tiles/GayTile", order = 2)]
public class GayTile : BreakableBrickTile
{
    public string resultTile;
    public override bool Interact(MonoBehaviour interacter, InteractionDirection direction, Vector3 worldLocation)
    {
        if (base.Interact(interacter, direction, worldLocation))
            return true;

        Vector3Int tileLocation = Utils.WorldToTilemapPosition(worldLocation);

        string spawnResult = "";
        if (interacter is PlayerController pl)
        {
            spawnResult = Utils.GetRandomItem(pl, true).prefab;
        } else
        {
            Powerup[] powerups = Resources.LoadAll<Powerup>("Scriptables/Powerups");
            List<string> availablePowerups = new List<string>();
            foreach (Powerup powerup in powerups)
            {
                if (powerup.state < Enums.PowerupState.Suit) continue;
                availablePowerups.Add(powerup.prefab);
            }

            spawnResult = availablePowerups[Random.Range(0, availablePowerups.Count)];
        }


        Bump(interacter, direction, worldLocation);

        object[] parametersBump = new object[] { tileLocation.x, tileLocation.y, direction == InteractionDirection.Down, resultTile, spawnResult };
        GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.BumpTile, parametersBump, ExitGames.Client.Photon.SendOptions.SendReliable);

        if (interacter is MonoBehaviourPun pun2)
            pun2.photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Powerup);
        return false;
    }
}
