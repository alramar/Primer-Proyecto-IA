using System;
using UnityEngine;


public class Door : MonoBehaviour
{
    public string KeyName = "key1";

    private void OnCollisionEnter(Collision other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        if (player == null)
            return;

        if (player.OwnKey(KeyName))
        {
            Destroy(gameObject);
        }
    }
}