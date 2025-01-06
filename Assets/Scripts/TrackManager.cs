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
    public struct ObstacleData
    {
        public int amount;
        public float ZPosition;
        public float XPosition;
        public float XPosition2;
        public bool isJump;
    }

    private const float UNITS_PER_SECOND = 10f;

    [SerializeField] private float difficulty = 1;

    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private float[] _obstaclePositions;
    [SerializeField] private GameObject _coin;
    [SerializeField] private GameObject _quietSegment;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _trail;
    [SerializeField] private GameObject _changeObstacle;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private Material _strongBeatMaterial;
    [SerializeField] private float _startDelayInSeconds = 3f; 
    [SerializeField] private float _actionDelayInSeconds = 0.3f; 
    [SerializeField] private float _maxObstaclesPerSecond = 2.5f;
    private float _obstaclesPerSecond;

    [SerializeField] private GameObject _obstacleParent;
    [SerializeField] private GameObject _markerParent;
    [SerializeField] private GameObject _quietSegmentParent;
    [SerializeField] private GameObject _changeObstacleParent;
    [SerializeField] private GameObject _coinParent;

    public AudioFeaturesLoader AudioFeaturesLoader;
    private AudioFeaturesLoader.AudioFeatures _loadedFeatures;

    private AudioSource audioSource;
    private float clipLength;
    [SerializeField] PlayerController playerController;

    private List<ChangeData> _changeData = new List<ChangeData>();
    private List<float> _segmentChangeTimes = new List<float>();

    private List<ObstacleData> _obstacleData = new List<ObstacleData>();

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
        yield return StartCoroutine(PlaceCoins());
        StartCoroutine(PlayAudioWithDelay());
    }

    IEnumerator AdjustObstacleSpawnRate()
    {
        _segmentChangeTimes = _loadedFeatures.SegmentAverages.Select(segment => segment.SegmentEnd).ToList();
        Debug.Log("Segment change times: " + string.Join(", ", _segmentChangeTimes));
        Debug.Log("Segment change time count: " + _segmentChangeTimes.Count);
        Debug.Log("Count of segment averages: " + _loadedFeatures.SegmentAverages.Count);

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

        yield return null;
    }

    IEnumerator PlayAudioWithDelay()
    {
        Debug.Log("Start coroutine to play audio with delay");
        audioSource.clip = _audioClip;
        playerController.SetAudioLoaded();
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
        float lastCoinZPosition = _startDelayInSeconds * UNITS_PER_SECOND; // Start placing coins after the delay
        float lastCoinXPosition = 0;
        bool isSameObstacle = false;
        float lastObstaclePosition = 0;
        
        while (lastCoinZPosition < clipLength)
        {
            // Check if there's an obstacle already at the current position
            float CoinXPosition = 0;
            float CoinYPosition = 0.5f;
            
            foreach (var obstacle in _obstacleData)
            {
                // Calculate the position of the obstacle based on the beat time
                float obstaclePosition = obstacle.ZPosition;

                if (Mathf.Abs(obstaclePosition - lastCoinZPosition) < 3f) // Small threshold to avoid overlap
                {
                    if (obstacle.isJump)
                    {
                        CoinYPosition = 1.5f;
                    }
                    else if (obstacle.amount == 1)
                    {
                        // get random position from _obstaclePositions except for the one where the obstacle is
                        if (lastObstaclePosition != obstacle.XPosition)
                        {
                            CoinXPosition = _obstaclePositions.Where(x => x != obstacle.XPosition).ToArray()[Random.Range(0, _obstaclePositions.Length - 1)];
                            lastCoinXPosition = CoinXPosition;
                            lastObstaclePosition = obstacle.XPosition;
                        }
                        else
                        {
                            CoinXPosition = lastCoinXPosition;
                        }
                    }
                    else if (obstacle.amount == 2)
                    {
                        // get random position from _obstaclePositions except for the two where the obstacles are
                        if (lastObstaclePosition != obstacle.XPosition)
                        {
                            CoinXPosition = _obstaclePositions.Where(x => x != obstacle.XPosition && x != obstacle.XPosition2).ToArray()[Random.Range(0, _obstaclePositions.Length - 2)];
                            lastCoinXPosition = CoinXPosition;
                            lastObstaclePosition = obstacle.XPosition;
                        }
                        else
                        {
                            CoinXPosition = lastCoinXPosition;
                        }
                    }
                }
            }

            SpawnCoin(lastCoinZPosition, CoinXPosition, CoinYPosition);

            // Increment the coin position by 1 unit
            lastCoinZPosition += 2f; 

            yield return null;
        }

        Debug.Log("Placed coins on the track");
        yield return null;
    }

    void SpawnCoin(float ZPosition, float XPosition = 0, float YPosition = 0.5f)
    {
        // Place a coin at the given position
        GameObject coin = Instantiate(_coin, new Vector3(XPosition, YPosition, ZPosition), Quaternion.identity, _coinParent.transform);
        coin.transform.localRotation = Quaternion.Euler(90, 0, 0);
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

        ObstacleData obstacleData = new ObstacleData
        {
            amount = 1,
            ZPosition = obstaclePosition,
            XPosition = 0,
            XPosition2 = 0,
            isJump = true
        };
        _obstacleData.Add(obstacleData);

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

            ObstacleData obstacleData = new ObstacleData
            {
                amount = 1,
                ZPosition = obstaclePosition,
                XPosition = _obstaclePositions[randomPositionIndex],
                XPosition2 = 0,
                isJump = false
            };
            _obstacleData.Add(obstacleData);

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

        ObstacleData obstacleData2 = new ObstacleData
        {
            amount = 2,
            ZPosition = obstaclePosition,
            XPosition = _obstaclePositions[randomPositionIndex],
            XPosition2 = _obstaclePositions[secondRandomPositionIndex],
            isJump = false
        };

        _obstacleData.Add(obstacleData2);

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