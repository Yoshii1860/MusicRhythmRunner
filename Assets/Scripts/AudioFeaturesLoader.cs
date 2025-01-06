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
        public List<SignificantChangeData> SignificantChangeData;
        public List<SegmentAverages> SegmentAverages;
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
    public struct Change
    {
        public float Start { get; set; }
        public float End { get; set; }
        public float StartRMS { get; set; }
        public float EndRMS { get; set; }
    }

    [System.Serializable]
    public struct SignificantChangeData
    {
        public Change Change { get; set; }
        public bool Louder { get; set; }
        public bool Faster { get; set; }
        public float PreAvgRMS { get; set; }
        public float PostAvgRMS { get; set; }
        public float PreTempo { get; set; }
        public float PostTempo { get; set; }
    }

    [System.Serializable]
    public struct QuietSegment 
    {
        public float Start;
        public float End;
        public float AvgRMS;
    }

    public struct SegmentAverages
    {
        public float SegmentStart;
        public float SegmentEnd;
        public float AvgRMS;
        public float AvgTempo;
    }

    public string jsonFilePath = "Assets/AudioAnalyzer/audio_features_Intocable.json";
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

                // Process SignificantChanges
                for (int i = 0; i < audioFeatures.SignificantChanges.Count; i++)
                {
                    List<float> changeData = audioFeatures.SignificantChanges[i];
                    if (changeData.Count >= 4) 
                    {
                        float start = changeData[0];
                        float rmsStart = changeData[1];
                        float end = changeData[2];
                        float rmsEnd = changeData[3];
                    }
                    else
                    {
                        Debug.LogError($"Invalid data for SignificantChanges at index {i}");
                        return null;
                    }
                }

                // Process SignificantChangeData
                for (int i = 0; i < audioFeatures.SignificantChangeData.Count; i++)
                {
                    SignificantChangeData changeData = audioFeatures.SignificantChangeData[i];
                    // Use the extracted data as needed
                    Debug.Log($"Significant Change Start: {changeData.Change.Start}, End: {changeData.Change.End}, StartRMS: {changeData.Change.StartRMS}, EndRMS: {changeData.Change.EndRMS}");
                    Debug.Log($"Louder: {changeData.Louder}, Faster: {changeData.Faster}, PreAvgRMS: {changeData.PreAvgRMS}, PostAvgRMS: {changeData.PostAvgRMS}, PreTempo: {changeData.PreTempo}, PostTempo: {changeData.PostTempo}");
                }

                // Process SegmentAverages
                for (int i = 0; i < audioFeatures.SegmentAverages.Count; i++)
                {
                    SegmentAverages segmentData = audioFeatures.SegmentAverages[i];
                    // Use the extracted data as needed
                    Debug.Log($"Segment Start: {segmentData.SegmentStart}, End: {segmentData.SegmentEnd}, AvgRMS: {segmentData.AvgRMS}, AvgTempo: {segmentData.AvgTempo}");
                }

                // Process QuietSegments
                for (int i = 0; i < audioFeatures.QuietSegments.Count; i++)
                {
                    List<float> quietSegmentData = audioFeatures.QuietSegments[i];
                    if (quietSegmentData.Count >= 3) 
                    {
                        float start = quietSegmentData[0];
                        float end = quietSegmentData[1];
                        float avgRMS = quietSegmentData[2];
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