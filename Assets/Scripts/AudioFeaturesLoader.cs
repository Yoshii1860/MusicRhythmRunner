using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioFeaturesLoader : MonoBehaviour
{
    [System.Serializable]
    public class AudioFeatures
    {
        public float Tempo;
        public List<float> BeatTimes;
        public List<float> TempoChanges;
        public List<float> TempoChangeTimes;
        public List<List<float>> SignificantChanges; 
        public List<List<float>> QuietSegments; // List of lists of floats
        public List<float> SpectralFlux;
        public List<float> BeatStrengths;
        public List<float> LoudnessPeakTimes;
        public List<float> SpectralCentroid;
        public List<float> SpectralCentroidTimes;
        //public List<float> Chroma;
        //public List<float> ChromaTimes;
    }

    [System.Serializable]
    public struct SignificantChange
    {
        public float Start;
        public float RMSStart;
        public float End;
        public float RMSEnd;
    }

    [System.Serializable]
    public struct QuietSegment 
    {
        public float Start;
        public float End;
        public float AvgRMS;
    }

    public string jsonFilePath = "Assets/AudioAnalyzer/audio_features.json";
    public AudioFeatures audioFeatures { get; private set; }

    public AudioFeatures LoadAudioFeatures()
    {
        if (File.Exists(jsonFilePath))
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            audioFeatures = JsonConvert.DeserializeObject<AudioFeatures>(jsonContent);

            if (audioFeatures != null)
            {
                Debug.Log("Audio features loaded successfully.");
                Debug.Log($"Tempo: {audioFeatures.Tempo}");
                Debug.Log($"Significant changes count: {audioFeatures.SignificantChanges.Count}");

                // Process SignificantChanges (example)
                for (int i = 0; i < audioFeatures.SignificantChanges.Count; i++)
                {
                    List<float> changeData = audioFeatures.SignificantChanges[i];
                    if (changeData.Count >= 4) 
                    {
                        float start = changeData[0];
                        float rmsStart = changeData[1];
                        float end = changeData[2];
                        float rmsEnd = changeData[3];
                        // Use the extracted data as needed
                        Debug.Log($"Start: {start}, RMSStart: {rmsStart}, End: {end}, RMSEnd: {rmsEnd}");
                    }
                    else
                    {
                        Debug.LogError($"Invalid data for SignificantChanges at index {i}");
                        return null;
                    }
                }

                // Process QuietSegments (example)
                for (int i = 0; i < audioFeatures.QuietSegments.Count; i++)
                {
                    List<float> quietSegmentData = audioFeatures.QuietSegments[i];
                    if (quietSegmentData.Count >= 3) 
                    {
                        float start = quietSegmentData[0];
                        float end = quietSegmentData[1];
                        float avgRMS = quietSegmentData[2];
                        // Use the extracted data as needed
                        Debug.Log($"Quiet Segment Start: {start}, End: {end}, AvgRMS: {avgRMS}");
                    }
                    else
                    {
                        Debug.LogError($"Invalid data for QuietSegments at index {i}");
                        return null;
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to load audio features.");
                return null;
            }
        }
        else
        {
            Debug.LogError($"JSON file not found at: {jsonFilePath}");
            return null;
        }


        return audioFeatures;
    }
}