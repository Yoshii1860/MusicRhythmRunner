using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class AudioFeaturesData
{
    public List<float> BeatTimes;
    public float Tempo;
    public List<float> BeatStrengths;
}

public class TrackManager : MonoBehaviour
{
    private const float UNITS_PER_SECOND = 10f;

    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private float[] _obstaclePositions;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _trail;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private Material _strongBeatMaterial;
    [SerializeField] private float _startDelayInSeconds = 3f; 
    [SerializeField] private float _actionDelayInSeconds = 0.3f; 
    [SerializeField] private float _maxObstaclesPerSecond = 2f;
    private float _obstaclesPerSecond;

    private List<float> _beatTimes;
    private List<float> _beatStrengths;
    private float _tempo;

    private AudioSource audioSource;
    private float clipLength;
    [SerializeField] PlayerController playerController;

    void Awake()
    {
        clipLength = _audioClip.length * UNITS_PER_SECOND + (_startDelayInSeconds * UNITS_PER_SECOND);
        audioSource = GetComponent<AudioSource>();
    }

    IEnumerator InitializeGame()
    {
        yield return StartCoroutine(LoadAudioFeatures());
        yield return StartCoroutine(AdjustObstacleSpawnRate());
        yield return StartCoroutine(SetupTrail());
        yield return StartCoroutine(PlaceObstacles());
        StartCoroutine(PlayAudioWithDelay());
    }

    IEnumerator LoadAudioFeatures()
    {
        //string jsonFilePath = @"C:\PythonProject\audio_features.json";
        string jsonFilePath = @"C:\PythonProject\audio_features.json";

        try
        {
            string jsonString = File.ReadAllText(jsonFilePath);
            var jsonData = JsonUtility.FromJson<AudioFeaturesData>(jsonString);
            _beatTimes = jsonData.BeatTimes;
            _tempo = jsonData.Tempo;
            _beatStrengths = jsonData.BeatStrengths;
            Debug.Log("BeatTimes length: " + _beatTimes.Count);
            Debug.Log("BeatStrengths length: " + _beatStrengths.Count);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading beat times from JSON: " + e.Message);
        }

        Debug.Log("Loaded data from JSON");
        Debug.Log("Tempo: " + _tempo);

        yield return null;
    }

    IEnumerator AdjustObstacleSpawnRate()
    {
        if (_tempo > 0)
        {
            _obstaclesPerSecond = Mathf.Min(_tempo / 60f, _maxObstaclesPerSecond);
        }

        Debug.Log("Adjusted obstacle spawn rate to " + _obstaclesPerSecond + " obstacles per second");

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

        Instantiate(_marker, new Vector3(0, 0.1f, _startDelayInSeconds * UNITS_PER_SECOND), Quaternion.identity);

        GameObject markerObject = null;
        GameObject obstacle = null;
        GameObject[] obstacles = new GameObject[2]; 
        bool isStrongBeat = false;

        for (int i = 0; i < _beatTimes.Count; i++)
        {
            if (_beatStrengths[i] < 2f)
            {
                continue;
            }

            if (_beatTimes[i] - lastObstacleTime >= 1f / _obstaclesPerSecond)
            {
                // Calculate obstacle position on the track
                float obstaclePosition = _beatTimes[i] * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

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
                markerObject = Instantiate(_marker, new Vector3(0, 0.1f, obstaclePosition), Quaternion.identity);

                // Set marker color based on beat strength
                if (_beatStrengths[i] > 10f)
                {
                    Debug.Log("Strong beat at " + _beatTimes[i]);
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

                lastObstacleTime = _beatTimes[i];
            }
            else if (_beatStrengths[i] > 10f && !isStrongBeat)
            {
                Destroy(markerObject);
                Destroy(obstacle);
                foreach (GameObject obstacleChild in obstacles)
                {
                    Destroy(obstacleChild);
                }

                // Calculate obstacle position on the track
                float obstaclePosition = _beatTimes[i] * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

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
                markerObject = Instantiate(_marker, new Vector3(0, 0.1f, obstaclePosition), Quaternion.identity);

                // Set marker color based on beat strength
                if (_beatStrengths[i] > 10f)
                {
                    Debug.Log("Strong beat at " + _beatTimes[i]);
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

                lastObstacleTime = _beatTimes[i];
            }
        }

        Debug.Log("Placed obstacles on the track");
        yield return null;
    }

    GameObject SpawnJumpObstacle(int obstacleIndex, float obstaclePosition)
    {
        GameObject jumpObstacle = Instantiate(_obstacles[0], new Vector3(0, _obstacles[obstacleIndex].transform.position.y, obstaclePosition), Quaternion.identity);
        return jumpObstacle;
    }

    GameObject[] SpawnMoveObstacle(int obstacleIndex, float obstaclePosition)
    {
        // Determine the number of obstacles to spawn (1 or 2)
        int randomAmountIndex = Random.Range(1, 3);

        // Choose a random position for the obstacle
        int randomPositionIndex = Random.Range(0, _obstaclePositions.Length);

        if (randomAmountIndex < 0)
        {
            // Instantiate a single obstacle
            GameObject obstacle = Instantiate(_obstacles[obstacleIndex], 
                                            new Vector3(_obstaclePositions[randomPositionIndex], _obstacles[obstacleIndex].transform.position.y, obstaclePosition), 
                                            Quaternion.identity);
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
                                            Quaternion.identity);
            obstacles[i] = obstacle;
        }

        return obstacles;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(InitializeGame());
    }

    // Update is called once per frame
    void Update()
    {
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