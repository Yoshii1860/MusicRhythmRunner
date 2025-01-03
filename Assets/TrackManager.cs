using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class BeatTimesData
{
    public List<float> beat_times;
}

public class TrackManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _obstacles;
    [SerializeField] private float[] _obstaclePositions;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _trail;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _lengthMultiplierInUnits = 10f;
    [SerializeField] private float maxObstaclesPerSecond = 2f;
    [SerializeField] private float _startDelayInUnits = 30f; 
    [SerializeField] private float _actionDelayInUnits = 3f; 

    private List<float> beatTimes;

    private AudioSource audioSource;
    private float clipLength;

    void Awake()
    {
        clipLength = _audioClip.length * _lengthMultiplierInUnits + _startDelayInUnits;

        GameObject newTrail = Instantiate(_trail, new Vector3(0, 0, 0.5f), Quaternion.identity);
        newTrail.transform.position = new Vector3(newTrail.transform.position.x, newTrail.transform.position.y, newTrail.transform.position.z * clipLength);
        newTrail.transform.localScale = new Vector3(1, 1, newTrail.transform.localScale.z * clipLength);

        LoadBeatTimes();
    }

    void LoadBeatTimes()
    {
        string jsonFilePath = @"C:\PythonProject\audio_features.json"; // Replace with the actual path

        try
        {
            string jsonString = File.ReadAllText(jsonFilePath);
            var jsonData = JsonUtility.FromJson<BeatTimesData>(jsonString);
            beatTimes = jsonData.beat_times;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading beat times from JSON: " + e.Message);
            beatTimes = new List<float>(); // Handle the case where loading fails
        }
    }

    void PlaceObstacles()
    {
        float lastObstacleTime = 0f;

        foreach (float beatTime in beatTimes)
        {
            if (beatTime - lastObstacleTime >= 1f / maxObstaclesPerSecond)
            {
                // Calculate obstacle position on the track
                float markerPosition = beatTime * _lengthMultiplierInUnits + _startDelayInUnits;

                Instantiate(_marker, new Vector3(0, 0.1f, markerPosition), Quaternion.identity);
                
                float obstaclePosition = beatTime * _lengthMultiplierInUnits + _startDelayInUnits + _actionDelayInUnits;

                // choose a random obstacle from the list
                int randomObstacleIndex = Random.Range(0, _obstacles.Length);
                int randomObstaclePositionIndex = 0;
                int secondRandomObstaclePositionIndex = 0;
                int randomObstacleCountIndex = 1;
                // if the random obstacle is the moveObstacle, choose a random position for it and random another obstacle
                if (randomObstacleIndex > 0)
                {
                    // choose a random position for the first obstacle
                    randomObstaclePositionIndex = Random.Range(0, _obstaclePositions.Length);
                    // choose if there is a second obstacle
                    randomObstacleCountIndex = Random.Range(1, 3);
                    // if there is a second obstacle, choose a random position for it
                    if (randomObstacleCountIndex > 1)
                    {
                        // choose a random position for the second obstacle
                        secondRandomObstaclePositionIndex = Random.Range(0, _obstaclePositions.Length);
                        // if the second obstacle is in the same position as the first, choose another position
                        if (randomObstaclePositionIndex == secondRandomObstaclePositionIndex)
                        {
                            // choose a random position for the second obstacle avoiding the position of the first obstacle
                            secondRandomObstaclePositionIndex = (randomObstaclePositionIndex + 1) % _obstaclePositions.Length;
                        }
                    }
                }
                else
                {
                    // choose a random position for the first obstacle
                    randomObstaclePositionIndex = 0;
                }
                // create the obstacles array for instantiation
                float[] randomObstaclePositions = { _obstaclePositions[randomObstaclePositionIndex], _obstaclePositions[secondRandomObstaclePositionIndex] };

                for (int i = 0; i < randomObstacleCountIndex; i++)
                {
                    GameObject obstacle = Instantiate(_obstacles[randomObstacleIndex],
                                                    new Vector3(randomObstaclePositions[i], _obstacles[randomObstacleIndex].transform.position.y, obstaclePosition),
                                                    Quaternion.identity);
                }

                lastObstacleTime = beatTime;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlaceObstacles();
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayAudioWithDelay());
    }

    // Update is called once per frame
    void Update()
    {
    }

    // IEnumerator requires specifying the type argument (void in this case)
    IEnumerator PlayAudioWithDelay()
    {
        yield return new WaitForSeconds(_startDelayInUnits / _lengthMultiplierInUnits);
        audioSource.clip = _audioClip;
        audioSource.Play();
    }
}