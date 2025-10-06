using System;
using UnityEngine;


public class Key : MonoBehaviour
{
    public string KeyName = "key1";

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        //this wasn't a player
        if (player == null)
            return;
    
        player.AddKey(KeyName);
        Destroy(gameObject);
    }
}
