using UnityEngine;

public class PickupCoin : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.CoinCount++;

            // Destroy coin
            Destroy(gameObject);
        }
    }
}
