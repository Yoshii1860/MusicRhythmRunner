using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    //Singleton Pattern
    public static GameManager Instance { get; private set; }

    public int CoinCount = 0;

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI timeStampText;
    [SerializeField] private PlayerController playerController;

    private AudioSource trackManagerAudioSource;

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
        trackManagerAudioSource = trackManager.GetComponent<AudioSource>();
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

    void Update()
    {
        coinText.text = CoinCount.ToString();
        // turn the current time of the track into a string 00:00 format and display it
        if (trackManagerAudioSource.isPlaying)
        {
            timeStampText.text = string.Format("{0}:{1:00}", (int)trackManagerAudioSource.time / 60, (int)trackManagerAudioSource.time % 60);
        }
    }

    public void EndGame(bool hasWon)
    {
        if (hasWon)
        {
            Debug.Log("You Win!");
            playerController.IsRunning = false;
        }
        else
        {
            Debug.Log("You Lose!");
        }
    }
}
