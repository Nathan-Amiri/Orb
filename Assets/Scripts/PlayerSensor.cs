using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Orb")) return;

        Orb orb = col.GetComponent<Orb>();

        //order orbisdestined first so that orb is removed from destinedorbs whether ready or not
        //if (OrbIsDestined(orb) || orb.ready)
        //    GetOrb(orb);
    }

    //private bool OrbIsDestined(Orb newOrb)
    //{
    //    if (newOrb == yellowDestinedOrb)
    //    {
    //        yellowDestinedOrb = null;
    //        return true;
    //    }
    //    //orb cannot be both yellow destined and green/purple destined
    //    foreach (Orb orb in destinedOrbs)
    //        if (newOrb == orb)
    //        {
    //            destinedOrbs.Remove(orb);
    //            return true;
    //        }
    //    return false;
    //}
}