using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TrackManager : MonoBehaviour
{
    private const float UNITS_PER_SECOND = 10f;

    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private float[] _obstaclePositions;
    [SerializeField] private GameObject _quietSegment;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _trail;
    [SerializeField] private GameObject _changeObstacle;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private Material _strongBeatMaterial;
    [SerializeField] private float _startDelayInSeconds = 3f; 
    [SerializeField] private float _actionDelayInSeconds = 0.3f; 
    [SerializeField] private float _maxObstaclesPerSecond = 2f;
    private float _obstaclesPerSecond;

    [SerializeField] private GameObject _obstacleParent;
    [SerializeField] private GameObject _markerParent;
    [SerializeField] private GameObject _quietSegmentParent;
    [SerializeField] private GameObject _changeObstacleParent;

    public AudioFeaturesLoader AudioFeaturesLoader;
    private AudioFeaturesLoader.AudioFeatures _loadedFeatures;

    private AudioSource audioSource;
    private float clipLength;
    [SerializeField] PlayerController playerController;

    bool inQuietSegment = false;

    void Awake()
    {
        // Load audio features
        AudioFeaturesLoader AudioFeaturesLoader = FindAnyObjectByType<AudioFeaturesLoader>();
        
        clipLength = _audioClip.length * UNITS_PER_SECOND + (_startDelayInSeconds * UNITS_PER_SECOND);
        audioSource = GetComponent<AudioSource>();
    }

    public void SetUpWorld(AudioFeaturesLoader.AudioFeatures audioFeatures)
    {
        _loadedFeatures = audioFeatures;
        if (_loadedFeatures == null)
        {
            Debug.LogError("Audio features not loaded.");
            return;
        }
        else
        {
            StartCoroutine(InitializeGame());
        }
    }
    
    IEnumerator InitializeGame()
    {
        yield return StartCoroutine(AdjustObstacleSpawnRate());
        yield return StartCoroutine(SetupTrail());
        yield return StartCoroutine(PlaceObstacles());
        StartCoroutine(PlayAudioWithDelay());
    }

    IEnumerator AdjustObstacleSpawnRate()
    {
        if (_loadedFeatures.Tempo > 0)
        {
            _obstaclesPerSecond = Mathf.Min(_loadedFeatures.Tempo / 60f, _maxObstaclesPerSecond);
        }

        yield return null;
    }

    IEnumerator SetupTrail()
    {
        GameObject newTrail = Instantiate(_trail, new Vector3(0, 0, 0.5f), Quaternion.identity);
        newTrail.transform.position = new Vector3(newTrail.transform.position.x, newTrail.transform.position.y, newTrail.transform.position.z * clipLength);
        newTrail.transform.localScale = new Vector3(1, 1, newTrail.transform.localScale.z * clipLength);

        yield return null;
    }

    IEnumerator PlaceObstacles()
    {
        float lastObstacleTime = 0f;

        Instantiate(_marker, new Vector3(0, 0.1f, _startDelayInSeconds * UNITS_PER_SECOND), Quaternion.identity, _markerParent.transform);

        GameObject markerObject = null;
        GameObject obstacle = null;
        GameObject[] obstacles = new GameObject[2]; 
        bool isStrongBeat = false;

        foreach (List<float> change in _loadedFeatures.SignificantChanges)
        {
            float changeTime = change[0];
            float changePosition = changeTime * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

            Instantiate(_changeObstacle, new Vector3(0, 0.1f, changePosition), Quaternion.identity, _changeObstacleParent.transform);
        }

        for (int i = 0; i < _loadedFeatures.BeatTimes.Count; i++)
        {
            if (_loadedFeatures.BeatStrengths[i] < 2f)
            {
                continue;
            }

            GameObject newQuietSegment = null;
            if (!inQuietSegment)
            {
                foreach (List<float> quietSegment in _loadedFeatures.QuietSegments)
                {
                    if (_loadedFeatures.BeatTimes[i] >= quietSegment[0] && _loadedFeatures.BeatTimes[i] <= quietSegment[1])
                    {
                        newQuietSegment = Instantiate(_quietSegment, new Vector3(0, 0.1f, 0), Quaternion.identity, _quietSegmentParent.transform);

                        // Set the length of the quiet segment based on the start and end times
                        float quietSegmentLength = (quietSegment[1] - quietSegment[0]);
                        newQuietSegment.transform.localScale = new Vector3(1, 1, quietSegmentLength);

                        // Set the position of the quiet segment based on the start time, delay, and consider the length of the segment
                        float quietSegmentStartPosition = (quietSegment[0] * UNITS_PER_SECOND) 
                                                        + (_startDelayInSeconds * UNITS_PER_SECOND) 
                                                        + ((newQuietSegment.transform.localScale.z / 2f) * UNITS_PER_SECOND);
                        newQuietSegment.transform.position = new Vector3(newQuietSegment.transform.position.x, newQuietSegment.transform.position.y, quietSegmentStartPosition);

                        inQuietSegment = true;
                        break;
                    }
                }
            }

            if (newQuietSegment != null)
            {
                continue;
            }
            else if (inQuietSegment)
            {
                inQuietSegment = false;
            }

            if (_loadedFeatures.BeatTimes[i] - lastObstacleTime >= 1f / _obstaclesPerSecond)
            {
                // Calculate obstacle position on the track
                float obstaclePosition = _loadedFeatures.BeatTimes[i] * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

                /*
                    Set Dificulty Level depending on beat strength
                    if (_beatStrengths[i] > 10f)
                    {
                        something more dificult
                    }
                    else
                    {
                        something easier
                    }

                */

                // Instantiate marker 
                markerObject = Instantiate(_marker, new Vector3(0, 0.1f, obstaclePosition), Quaternion.identity, _markerParent.transform);

                // Set marker color based on beat strength
                if (_loadedFeatures.BeatStrengths[i] > 10f)
                {
                    isStrongBeat = true;
                    markerObject.GetComponent<Renderer>().material = _strongBeatMaterial;
                }

                // Calculate obstacle position with delay
                obstaclePosition += (_actionDelayInSeconds * UNITS_PER_SECOND); 

                // Choose a random obstacle
                int randomObstacleIndex = Random.Range(0, _obstacles.Length);

                switch (_obstacles[randomObstacleIndex].name)
                {
                    case "JumpObstaclePrefab":
                        obstacle = SpawnJumpObstacle(randomObstacleIndex, obstaclePosition);
                        break;
                    case "MoveObstaclePrefab":
                        obstacles = SpawnMoveObstacle(randomObstacleIndex, obstaclePosition);
                        break;
                    default:
                        break;
                }

                lastObstacleTime = _loadedFeatures.BeatTimes[i];
            }
            else if (_loadedFeatures.BeatStrengths[i] > 10f && !isStrongBeat)
            {
                Destroy(markerObject);
                Destroy(obstacle);
                foreach (GameObject obstacleChild in obstacles)
                {
                    Destroy(obstacleChild);
                }

                // Calculate obstacle position on the track
                float obstaclePosition = _loadedFeatures.BeatTimes[i] * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

                /*
                    Set Dificulty Level depending on beat strength
                    if (_beatStrengths[i] > 10f)
                    {
                        something more dificult
                    }
                    else
                    {
                        something easier
                    }

                */

                // Instantiate marker 
                markerObject = Instantiate(_marker, new Vector3(0, 0.1f, obstaclePosition), Quaternion.identity, _markerParent.transform);

                // Set marker color based on beat strength
                if (_loadedFeatures.BeatStrengths[i] > 10f)
                {
                    Debug.Log("Strong beat at " + _loadedFeatures.BeatTimes[i]);
                    markerObject.GetComponent<Renderer>().material = _strongBeatMaterial;
                }

                // Calculate obstacle position with delay
                obstaclePosition += (_actionDelayInSeconds * UNITS_PER_SECOND); 

                // Choose a random obstacle
                int randomObstacleIndex = Random.Range(0, _obstacles.Length);

                switch (_obstacles[randomObstacleIndex].name)
                {
                    case "JumpObstaclePrefab":
                        obstacle = SpawnJumpObstacle(randomObstacleIndex, obstaclePosition);
                        break;
                    case "MoveObstaclePrefab":
                        obstacles = SpawnMoveObstacle(randomObstacleIndex, obstaclePosition);
                        break;
                    default:
                        break;
                }

                lastObstacleTime = _loadedFeatures.BeatTimes[i];
            }
        }

        Debug.Log("Placed obstacles on the track");
        yield return null;
    }

    GameObject SpawnJumpObstacle(int obstacleIndex, float obstaclePosition)
    {
        GameObject jumpObstacle = Instantiate(_obstacles[0], new Vector3(0, _obstacles[obstacleIndex].transform.position.y, obstaclePosition), Quaternion.identity, _obstacleParent.transform);
        return jumpObstacle;
    }

    GameObject[] SpawnMoveObstacle(int obstacleIndex, float obstaclePosition)
    {
        // Determine the number of obstacles to spawn (1 or 2)
        int randomAmountIndex = Random.Range(1, 3);

        // Choose a random position for the obstacle
        int randomPositionIndex = Random.Range(0, _obstaclePositions.Length);

        if (randomAmountIndex == 1)
        {
            // Instantiate a single obstacle
            GameObject obstacle = Instantiate(_obstacles[obstacleIndex], 
                                            new Vector3(_obstaclePositions[randomPositionIndex], _obstacles[obstacleIndex].transform.position.y, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);
            return new GameObject[] { obstacle };
        }

        // Choose a second random position for the obstacle - must be different from the first
        int secondRandomPositionIndex = Random.Range(0, _obstaclePositions.Length);
        if (randomPositionIndex == secondRandomPositionIndex)
        {
            secondRandomPositionIndex = (secondRandomPositionIndex + 1) % _obstaclePositions.Length;
        }

        int[] randomPositions = new int[] { randomPositionIndex, secondRandomPositionIndex };

        GameObject[] obstacles = new GameObject[randomAmountIndex];

        for (int i = 0; i < randomAmountIndex; i++)
        {
            // Instantiate the obstacle
            GameObject obstacle = Instantiate(_obstacles[obstacleIndex], 
                                            new Vector3(_obstaclePositions[randomPositions[i]], _obstacles[obstacleIndex].transform.position.y, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);
            obstacles[i] = obstacle;
        }

        return obstacles;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(InitializeGame());
    }

    // IEnumerator requires specifying the type argument (void in this case)
    IEnumerator PlayAudioWithDelay()
    {
        Debug.Log("Start coroutine to play audio with delay");
        audioSource.clip = _audioClip;
        playerController.SetAudioLoaded();
        yield return new WaitForSeconds(_startDelayInSeconds);
        audioSource.Play();
        Debug.Log("Audio started playing");
    }
}