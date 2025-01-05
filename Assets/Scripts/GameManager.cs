using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
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
