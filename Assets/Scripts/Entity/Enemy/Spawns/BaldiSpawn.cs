using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldiSpawn : EnemySpawnpoint
{
    // Start is called before the first frame update
    public override bool AttemptSpawning()
    {
        if (currentEntity != null)
        {
            Baldi b = currentEntity.GetComponent<Baldi>();
            if (b != null)
                b.GetMad();
        }
            
        return base.AttemptSpawning();
    }
}
