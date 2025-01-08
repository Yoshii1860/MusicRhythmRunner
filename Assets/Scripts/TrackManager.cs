using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TrackManager : MonoBehaviour
{
    [System.Serializable]
    public struct ChangeData
    {
        public float Start;
        public float End;
        public bool Louder;
        public bool Faster;
        public float RMSChange;
        public float TempoChange;
    }

    [System.Serializable]
    public struct CoinData
    {
        public int amount;
        public float ZPosition;
        public float AvailableXPosition;
        public bool isJump;
    }

    private const float UNITS_PER_SECOND = 10f;

    [Header("Game Settings")]
    [SerializeField] private float difficulty = 1;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _startDelayInSeconds = 3f; 
    [SerializeField] private float _endDelayInSeconds = 4f;
    [SerializeField] private float _actionDelayInSeconds = 0.3f; 
    [SerializeField] private float _maxObstaclesPerSecond = 2.5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject _trail;
    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private GameObject _coin;
    [SerializeField] private GameObject _quietSegment;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _changeObstacle;
    [SerializeField] private GameObject _treasure;
    [SerializeField] private GameObject _finishTrigger;
    [Space(10)]

    [Header("Parent Objects")]
    [SerializeField] private GameObject _obstacleParent;
    [SerializeField] private GameObject _markerParent;
    [SerializeField] private GameObject _quietSegmentParent;
    [SerializeField] private GameObject _changeObstacleParent;
    [SerializeField] private GameObject _coinParent;

    [Header("Other Settings")]
    [SerializeField] private float[] _obstaclePositions;
    [SerializeField] private Material _strongBeatMaterial;
    public AudioFeaturesLoader AudioFeaturesLoader;
    [SerializeField] private GameObject _player;

    private float _obstaclesPerSecond;
    private AudioFeaturesLoader.AudioFeatures _loadedFeatures;
    private AudioSource audioSource;
    private float clipLength;
    private PlayerController _playerController;
    private Animator _playerAnimator;
    private List<ChangeData> _changeData = new List<ChangeData>();
    private List<float> _segmentChangeTimes = new List<float>();
    private List<CoinData> _coinData = new List<CoinData>();
    bool inQuietSegment = false;

    [SerializeField] private Material groundMaterial; // Assign your material here
    [SerializeField] private string textureProperty = "_MainTex"; // Default texture property for the texture

    void SetTextureScale(float textureSize)
    {
        if (groundMaterial != null && groundMaterial.HasProperty(textureProperty))
        {
            // Calculate tiling based on the size of the ground and texture size
            Vector2 tiling = new Vector2(1, textureSize);

            // Apply the tiling to the material
            groundMaterial.SetTextureScale(textureProperty, tiling);
        }
    }

    void Awake()
    {
        // Load audio features
        AudioFeaturesLoader AudioFeaturesLoader = FindAnyObjectByType<AudioFeaturesLoader>();
        
        clipLength = _audioClip.length * UNITS_PER_SECOND + (_startDelayInSeconds * UNITS_PER_SECOND) + (_endDelayInSeconds * UNITS_PER_SECOND);
        audioSource = GetComponent<AudioSource>();
        _playerController = _player.GetComponent<PlayerController>();
        _playerAnimator = _player.GetComponentInChildren<Animator>();
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
        yield return StartCoroutine(PlaceCoins());
        StartCoroutine(PlayAudioWithDelay());
    }

    IEnumerator AdjustObstacleSpawnRate()
    {
        _segmentChangeTimes = _loadedFeatures.SegmentAverages.Select(segment => segment.SegmentEnd).ToList();

        if (_loadedFeatures.Tempo > 0)
        {
            ChangeDifficultyOnChange(true);
        }

        yield return null;
    }

    IEnumerator SetupTrail()
    {
        GameObject newTrail = Instantiate(_trail, new Vector3(0, 0, 0.5f), Quaternion.identity);
        newTrail.transform.position = new Vector3(newTrail.transform.position.x, newTrail.transform.position.y, newTrail.transform.position.z * clipLength);
        newTrail.transform.localScale = new Vector3(1, 1, newTrail.transform.localScale.z * clipLength);

        SetTextureScale(clipLength/10f);

        yield return null;
    }

    IEnumerator PlayAudioWithDelay()
    {
        Debug.Log("Start coroutine to play audio with delay");
        audioSource.clip = _audioClip;
        _playerController.SetAudioLoaded();
        _playerAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(_startDelayInSeconds);
        audioSource.Play();
        Debug.Log("Audio started playing");
    }

    void ChangeDifficultyOnChange(bool isFirstSegment = false)
    {
        // change effects based on loudness

        // change speed based on tempo
        _obstaclesPerSecond = Mathf.Min((_loadedFeatures.SegmentAverages.First().AvgTempo / 60f) * difficulty, _maxObstaclesPerSecond);

        if (!isFirstSegment)
        {
            _loadedFeatures.SegmentAverages.RemoveAt(0);
        }
    }

    IEnumerator PlaceCoins()
    {
        float lastCoinZPosition = _startDelayInSeconds * UNITS_PER_SECOND; // Start placing coins after the _endDelayInSeconds
        float clipLengthWithoutEndDelay = clipLength - (_endDelayInSeconds * UNITS_PER_SECOND); // Don't place coins after the _endDelayInSeconds
        GameObject lastCoin = null;

        while (lastCoinZPosition < clipLengthWithoutEndDelay)
        {
            // Check if there's an obstacle already at the current position
            float newCoinXPosition = 0;
            float CoinYPosition = 0.5f;
            
            foreach (var coin in _coinData)
            {
                // Calculate the position of the obstacle based on the beat time
                float obstaclePosition = coin.ZPosition;

                if (Mathf.Abs(obstaclePosition - lastCoinZPosition) < 3f) // Small threshold to avoid overlap
                {
                    if (coin.isJump)
                    {
                        CoinYPosition = 1.5f;
                    }
                    else if (coin.amount == 1)
                    {
                        newCoinXPosition = coin.AvailableXPosition;
                    }
                    else if (coin.amount == 2)
                    {
                        newCoinXPosition = coin.AvailableXPosition;
                    }
                }
            }

            lastCoin = SpawnCoin(lastCoinZPosition, newCoinXPosition, CoinYPosition);

            // Increment the coin position by 1 unit
            lastCoinZPosition += 2f; 

            yield return null;
        }

        Instantiate(_finishTrigger, new Vector3(0, 0.1f, lastCoinZPosition), Quaternion.identity);
        Instantiate(_treasure, new Vector3(0, 0.1f, lastCoinZPosition + UNITS_PER_SECOND), Quaternion.identity);

        Debug.Log("Placed coins on the track");
        yield return null;
    }

    GameObject SpawnCoin(float ZPosition, float XPosition = 0, float YPosition = 0.5f)
    {
        // Place a coin at the given position
        return Instantiate(_coin, new Vector3(XPosition, YPosition, ZPosition), Quaternion.identity, _coinParent.transform);
    }

    IEnumerator PlaceObstacles()
    {
        float lastObstacleTime = 0f;

        Instantiate(_marker, new Vector3(0, 0.1f, _startDelayInSeconds * UNITS_PER_SECOND), Quaternion.identity, _markerParent.transform);

        SpawnSignificantChangeObstacles();

        for (int i = 0; i < _loadedFeatures.BeatTimes.Count; i++)
        {
            // if segment average is reached, change difficulty and remove the segment from the list
            if (_segmentChangeTimes.Count > 0 && _loadedFeatures.BeatTimes[i] >= _segmentChangeTimes[0])
            {
                ChangeDifficultyOnChange();
                _segmentChangeTimes.RemoveAt(0);
            }

            // if beat is too low, skip
            if (_loadedFeatures.BeatStrengths[i] < 2f)
            {
                continue;
            }

            // check for quiet segments
            GameObject newQuietSegment = SpawnQuietSegment(i);

            // if inside quiet segment, skip
            if (newQuietSegment != null)
            {
                continue;
            }

            // place obstacles on the track
            if (_loadedFeatures.BeatTimes[i] - lastObstacleTime >= 1f / _obstaclesPerSecond)
            {
                CreateNewObstacle(i);

                lastObstacleTime = _loadedFeatures.BeatTimes[i];
            }
        }

        Debug.Log("Placed obstacles on the track");
        yield return null;
    }

    void CreateNewObstacle(int index)
    {
        // Calculate obstacle position on the track
        float obstaclePosition = _loadedFeatures.BeatTimes[index] * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

        // Instantiate marker 
        GameObject markerObject = Instantiate(_marker, new Vector3(0, 0.1f, obstaclePosition), Quaternion.identity, _markerParent.transform);

        // Calculate obstacle position with delay
        obstaclePosition += (_actionDelayInSeconds * UNITS_PER_SECOND); 

        // Set marker color based on beat strength
        if (_loadedFeatures.BeatStrengths[index] > 10f)
        {
            //isStrongBeat = true;
            markerObject.GetComponent<Renderer>().material = _strongBeatMaterial;
            SpawnJumpObstacle(0, obstaclePosition);
        }
        else
        {
            SpawnMoveObstacle(1, obstaclePosition);
        }
    }

    GameObject SpawnJumpObstacle(int obstacleIndex, float obstaclePosition)
    {
        GameObject jumpObstacle = Instantiate(_obstacles[0], new Vector3(0, _obstacles[obstacleIndex].transform.position.y, obstaclePosition), Quaternion.identity, _obstacleParent.transform);

        CoinData coinData = new CoinData
        {
            amount = 1,
            ZPosition = obstaclePosition,
            AvailableXPosition = 0,
            isJump = true
        };
        _coinData.Add(coinData);

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
                                            new Vector3(_obstaclePositions[randomPositionIndex], _obstacles[obstacleIndex].transform.position.y / 2, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);

            CoinData coinData = new CoinData
            {
                amount = 1,
                ZPosition = obstacle.transform.position.z,
                AvailableXPosition = _obstaclePositions.Where(x => x != obstacle.transform.position.x).First(),
                isJump = false
            };
            _coinData.Add(coinData);

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
                                            new Vector3(_obstaclePositions[randomPositions[i]], _obstacles[obstacleIndex].transform.position.y / 2, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);
            obstacles[i] = obstacle;
        }

        CoinData coinData2 = new CoinData
        {
            amount = 2,
            ZPosition = obstacles[0].transform.position.z,
            AvailableXPosition = _obstaclePositions.Where(x => x != obstacles[0].transform.position.x && x != obstacles[1].transform.position.x).First(),
            isJump = false
        };

        _coinData.Add(coinData2);

        return obstacles;
    }

    void SpawnSignificantChangeObstacles()
    {
        foreach (var singleChange in _loadedFeatures.SignificantChangeData)
        {
            float changeTime = singleChange.Change.Start;
            float changePosition = changeTime * UNITS_PER_SECOND + _startDelayInSeconds * UNITS_PER_SECOND;

            Instantiate(_changeObstacle, new Vector3(0, 0.1f, changePosition), Quaternion.identity, _changeObstacleParent.transform);

            ChangeData changeData = new ChangeData
            {
                Start = singleChange.Change.Start,
                End = singleChange.Change.End,
                Louder = singleChange.Louder,
                Faster = singleChange.Faster,
                RMSChange = singleChange.PostAvgRMS - singleChange.PreAvgRMS,
                TempoChange = singleChange.PostTempo - singleChange.PreTempo
            };
            _changeData.Add(changeData);
        }
    }

    GameObject SpawnQuietSegment(int index)
    {
        foreach (List<float> quietSegment in _loadedFeatures.QuietSegments)
        {
            if (_loadedFeatures.BeatTimes[index] >= quietSegment[0] && _loadedFeatures.BeatTimes[index] <= quietSegment[1])
            {
                if (!inQuietSegment)
                {
                    GameObject newQuietSegment = Instantiate(_quietSegment, new Vector3(0, 0.1f, 0), Quaternion.identity, _quietSegmentParent.transform);

                    // Set the length of the quiet segment based on the start and end times
                    float quietSegmentLength = (quietSegment[1] - quietSegment[0]);
                    newQuietSegment.transform.localScale = new Vector3(1, 1, quietSegmentLength);

                    // Set the position of the quiet segment based on the start time, delay, and consider the length of the segment
                    float quietSegmentStartPosition = (quietSegment[0] * UNITS_PER_SECOND) 
                                                    + (_startDelayInSeconds * UNITS_PER_SECOND) 
                                                    + ((newQuietSegment.transform.localScale.z / 2f) * UNITS_PER_SECOND);
                    newQuietSegment.transform.position = new Vector3(newQuietSegment.transform.position.x, newQuietSegment.transform.position.y, quietSegmentStartPosition);

                    inQuietSegment = true;
                    return newQuietSegment;
                }
                else
                {
                    return null;
                }
            }
        }
        inQuietSegment = false;
        return null;
    }
}