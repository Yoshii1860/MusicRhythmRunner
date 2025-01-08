using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    TreasureTrigger treasureTrigger;

    // OnTriggerEnter is called when the Collider other enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            treasureTrigger.OpenChest();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        treasureTrigger = FindAnyObjectByType<TreasureTrigger>();
    }
}
