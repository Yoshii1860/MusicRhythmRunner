using UnityEngine;

public class TreasureTrigger : MonoBehaviour
{
    Animator animator;

    // OnTriggerEnter is called when the Collider other enters the trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            GameManager.Instance.EndGame(true);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void OpenChest()
    {
        animator.SetBool("Open", true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
