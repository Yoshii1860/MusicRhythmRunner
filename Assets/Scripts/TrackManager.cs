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

    [Header("Game Settings")]
    [SerializeField] private float difficulty = 1;
    [SerializeField] private AudioClip _audioClip;
    private float clipLength;
    [SerializeField] private float _startDelayInSeconds = 3f; 
    [SerializeField] private float _endDelayInSeconds = 4f;
    [SerializeField] private float _actionDelayInSeconds = 0.3f; 
    [SerializeField] private float _maxObstaclesPerSecond = 2.5f;
    private float _obstaclesPerSecond;
    private const float UNITS_PER_SECOND = 10f;
    private const int COIN_DISTANCE = 2;
    private const int COLUMN_LENGTH = 3;

    [Header("Prefabs")]
    [SerializeField] private GameObject _trail;
    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private GameObject _jumpObstacle;
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
    [SerializeField] private Material groundMaterial;
    [SerializeField] private string textureProperty = "_MainTex";

    [Header("References")]
    private AudioFeaturesLoader.AudioFeatures _loadedFeatures;
    private AudioSource audioSource;
    private PlayerController _playerController;
    private Animator _playerAnimator;
    private MapGridManager _map;

    [Header("Data")]
    private List<ChangeData> _changeData = new List<ChangeData>();
    private List<float> _segmentChangeTimes = new List<float>();
    bool inQuietSegment = false;

    void Awake()
    {
        // Load audio features
        AudioFeaturesLoader AudioFeaturesLoader = FindAnyObjectByType<AudioFeaturesLoader>();
        
        clipLength = _audioClip.length * UNITS_PER_SECOND + (_startDelayInSeconds * UNITS_PER_SECOND) + (_endDelayInSeconds * UNITS_PER_SECOND);
        audioSource = GetComponent<AudioSource>();
        _playerController = _player.GetComponent<PlayerController>();
        _playerAnimator = _player.GetComponentInChildren<Animator>();

        _map = GetComponent<MapGridManager>();
        _map.InitializeGrid((int)clipLength, COLUMN_LENGTH);
    }

#region Initialization

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

#endregion

#region Difficulty Adjustments

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

#endregion

#region Obstacle Spawning

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
            SpawnJumpObstacle(obstaclePosition);
        }
        else
        {
            SpawnMoveObstacle(obstaclePosition);
        }
    }

    GameObject SpawnJumpObstacle(float obstaclePosition)
    {
        GameObject jumpObstacle = Instantiate(_jumpObstacle, new Vector3(0, _jumpObstacle.transform.position.y, obstaclePosition), Quaternion.identity, _obstacleParent.transform);

        int obstaclePositionInt = Mathf.RoundToInt(obstaclePosition);
        _map.AddJumpObstacle(obstaclePositionInt);

        return jumpObstacle;
    }

    GameObject[] SpawnMoveObstacle(float obstaclePosition)
    {
        // Determine the number of obstacles to spawn (1 or 2)
        int randomAmountIndex = Random.Range(1, 3);

        // Choose a random position for the obstacle
        int randomPositionIndex = Random.Range(0, _obstaclePositions.Length);

        // Choose a random obstacle from the list
        int randomObstacleIndex = Random.Range(0, _obstacles.Length);

        if (randomAmountIndex == 1)
        {
            // Instantiate a single obstacle
            GameObject obstacle = Instantiate(_obstacles[randomObstacleIndex], 
                                            new Vector3(_obstaclePositions[randomPositionIndex], _obstacles[randomObstacleIndex].transform.position.y / 2, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);


            int obstaclePositionInt = Mathf.RoundToInt(obstaclePosition);
            _map.AddObstacle(obstaclePositionInt, randomPositionIndex);

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
            randomObstacleIndex = Random.Range(0, _obstacles.Length);

            // Instantiate the obstacle
            GameObject obstacle = Instantiate(_obstacles[randomObstacleIndex], 
                                            new Vector3(_obstaclePositions[randomPositions[i]], _obstacles[randomObstacleIndex].transform.position.y / 2, obstaclePosition), 
                                            Quaternion.identity, _obstacleParent.transform);
            obstacles[i] = obstacle;

            int obstaclePositionInt = Mathf.RoundToInt(obstaclePosition);
            _map.AddObstacle(obstaclePositionInt, randomPositions[i]);
        }

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

                    newQuietSegment.GetComponent<EffectSpawner>().specificTime = quietSegmentLength;

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

#region Coin Spawning

    IEnumerator PlaceCoins()
    {
        int currentLane = 1;
        int targetLane = currentLane;
        float rawClipLength = _audioClip.length * UNITS_PER_SECOND;
        int coinDelay = 0;
        float startDelay = _startDelayInSeconds * UNITS_PER_SECOND;
        
        float lastXCoinPosition = 0;
        int coinCount = 0;

        for (int i = 0; i < rawClipLength; i++)
        {
            // Don´t place coins before the start delay
            if (i < startDelay)
            {
                continue;
            }
            // Don´t place coins for the end delay and prevent map index out of range
            else if (i >= rawClipLength - COIN_DISTANCE)
            {
                break;
            }

            // Don´t place coins too close to each other
            if (coinDelay < COIN_DISTANCE)
            {
                coinDelay++;
                continue;
            }
            coinDelay = 0;

            // If 10 coins are placed in a row, change the lane without taking the current position into account
            if (coinCount >= 10 && !_map.GetMapData(i + COIN_DISTANCE, currentLane).IsJump)
            {
                coinCount = 0;
                if (currentLane == 0 || currentLane == 2)
                {
                    targetLane = 1;
                }
                else if (currentLane == 1)
                {
                    int randomLane = Random.Range(0, 2);
                    targetLane = randomLane == 0 ? 0 : 2;
                }
            }

            // Change target lane if there is an obstacle in the current lane
            if (_map.GetMapData(i + COIN_DISTANCE, currentLane).IsOccupied)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    if (!_map.GetMapData(i + COIN_DISTANCE, lane).IsOccupied)
                    {
                        targetLane = lane;
                        break;
                    }
                }
            }
            // Change target lane if there is an obstacle close to the current lane
            else
            {
                if (_map.GetMapData(i + COIN_DISTANCE, currentLane).IsCloseToObstacle)
                {
                    for (int lane = 0; lane < 3; lane++)
                    {
                        if (!_map.GetMapData(i + COIN_DISTANCE, lane).IsCloseToObstacle)
                        {
                            targetLane = lane;
                            break;
                        }
                    }
                }
            }
            
            // Check if the current lane is different from the target lane
            if (currentLane != targetLane)
            {
                // Check if the target lane is two lanes away from the current lane
                if (Mathf.Abs(currentLane - targetLane) == 2)
                {
                    currentLane = currentLane < targetLane ? currentLane + 1 : currentLane - 1;
                }
                else
                {
                    currentLane = targetLane;
                }
            }

            // Check if the current lane is blocked by an obstacle and change the lane to unoccupied lane
            if (_map.GetMapData(i + COIN_DISTANCE, currentLane).IsOccupied && !_map.GetMapData(i + COIN_DISTANCE, currentLane).IsJump)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    if (!_map.GetMapData(i + COIN_DISTANCE, lane).IsOccupied)
                    {
                        Debug.Log("Coin is on: " + (i + COIN_DISTANCE));
                        Debug.Log("Coin placed on: " + currentLane);
                        currentLane = lane;
                        Debug.Log("Coin is now on: " + currentLane);
                        Debug.Log("Coin placed on obstacle and now changed lane");
                        break;
                    }
                }
            }

            float xPosition = _obstaclePositions[currentLane];

            // Change x position if the coin is too close to the end
            if (i + COIN_DISTANCE >= rawClipLength - 10)
            {
                xPosition = _obstaclePositions[1];
            }

            // Check if the last coin was placed at the same x position
            if (lastXCoinPosition == xPosition)
            {
                coinCount++;
            }
            else
            {
                coinCount = 0;
                lastXCoinPosition = xPosition;
            }

            if (_map.GetMapData(i + COIN_DISTANCE, currentLane).IsOccupied && !_map.GetMapData(i + COIN_DISTANCE, currentLane).IsJump)
            {
                Debug.LogError("Coin placed on occupied position at: " + (i + COIN_DISTANCE) + " and lane: " + currentLane);
            }

            SpawnCoin(i, xPosition, _map.GetMapData(i + COIN_DISTANCE, currentLane).IsJump ? 1.5f : 0.5f);
        }

        // Place Treasure at the end of the track
        Instantiate(_treasure, new Vector3(0, 0.5f, rawClipLength + COIN_DISTANCE), Quaternion.identity, _coinParent.transform);

        yield return null;
    }

    GameObject SpawnCoin(float ZPosition, float XPosition = 0, float YPosition = 0.5f)
    {
        // Place a coin at the given position
        return Instantiate(_coin, new Vector3(XPosition, YPosition, ZPosition), Quaternion.identity, _coinParent.transform);
    }

#endregion

# endregion
}