using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpeedItem : DungeonItem
{
    public float speedBonus = 15f;

    protected override void ApplyEffect(GameObject player)
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed += speedBonus;
            Debug.Log($"[SpeedItem] Speed increased by {speedBonus}. New speed: {agent.speed}");
        }
    }
}
