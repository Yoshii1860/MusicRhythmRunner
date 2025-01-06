using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Singleton Pattern
    public static GameManager Instance { get; private set; }

    public int CoinCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Load audio features
        AudioFeaturesLoader audioFeaturesLoader = FindAnyObjectByType<AudioFeaturesLoader>();
        // Initialize game
        TrackManager trackManager = FindAnyObjectByType<TrackManager>();
        AudioFeaturesLoader.AudioFeatures audioFeaturesLoaded = audioFeaturesLoader.LoadAudioFeatures();
        if (audioFeaturesLoaded != null)
        {
            trackManager.SetUpWorld(audioFeaturesLoaded);
        }
        else 
        {
            Debug.LogError("Failed to load audio features.");
        }
    }
}
