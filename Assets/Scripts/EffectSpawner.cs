using UnityEngine;
using System.Collections;

public class EffectSpawner : MonoBehaviour
{
    public GameObject[] effectPrefabs;
    public bool IsSpecificTime = false;
    public float specificTime = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            int random = Random.Range(0, effectPrefabs.Length);
            GameObject particleEffect = Instantiate(effectPrefabs[random], other.transform.position, Quaternion.identity, other.transform);
            Debug.Log("Effect spawned: " + particleEffect.name);
            Debug.Log("Effect duration: " + (IsSpecificTime ? specificTime : particleEffect.GetComponent<ParticleSystem>().main.duration));
            StartCoroutine(StopEffect(particleEffect, IsSpecificTime ? specificTime : particleEffect.GetComponent<ParticleSystem>().main.duration));
        }
    }

    IEnumerator StopEffect(GameObject effect, float time)
    {
        yield return new WaitForSeconds(time);
        ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop();
        }
        Debug.Log("Effect stopped: " + effect.name);
        Destroy(effect, 1f);
        yield return new WaitForSeconds(1f);
        Debug.Log("Effect destroyed: ");
    }
}
