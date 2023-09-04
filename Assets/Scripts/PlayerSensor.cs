using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private SensorRelay sensorRelay;
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Orb")) return;

        sensorRelay.SensorRelayServerRpc(col.GetComponent<Orb>());
    }
}