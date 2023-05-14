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

        string spawnResult = null;
        if (interacter is PlayerController pl)
        {
            var item = Utils.GetRandomItem(pl, true);
            if (item != null) spawnResult = item.prefab;
        } else
        {
            Utils.GetCustomProperty(Enums.NetRoomProperties.ModdedPowerups, out bool modded);
            if (modded)
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
        }


        if (spawnResult != null)
        {

            Utils.GetCustomProperty(Enums.NetRoomProperties.ChaosMode, out bool chaos);
            if (chaos && Random.Range(0, 50) == 32)
            {
                // spawn mio honda
                PhotonNetwork.InstantiateRoomObject("Prefabs/Enemy/Mio Honda", worldLocation + new Vector3(0.5f, 2.5f, 0f), Quaternion.identity);
                spawnResult = null;
            }

            Bump(interacter, direction, worldLocation);

            object[] parametersBump = new object[] { tileLocation.x, tileLocation.y, direction == InteractionDirection.Down, resultTile, spawnResult };
            GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.BumpTile, parametersBump, ExitGames.Client.Photon.SendOptions.SendReliable);

            if (interacter is MonoBehaviourPun pun2)
                pun2.photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Powerup);
        } else
        {
            Break(interacter, worldLocation, Enums.Sounds.World_Block_Break);
            return true;
        }
        return false;
    }
}
