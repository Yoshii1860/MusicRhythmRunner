import argparse
import librosa
import numpy as np
import json
    
def detect_significant_loudness_changes(y, sr, low_threshold=0.02, high_multiplier=5, min_duration=3.0, window_size=1024, min_spike_duration=0.05):
    # Step 1: Calculate RMS values for the audio
    rms = librosa.feature.rms(y=y, frame_length=window_size, hop_length=window_size//2)[0]
    
    # Step 2: Convert RMS to time values for easier interpretation
    times = librosa.times_like(rms, sr=sr)
    
    # Step 3: Identify low volume periods (below low_threshold for at least min_duration)
    low_volume_periods = []
    in_low_period = False
    low_start_time = None
    low_rms_values = []

    for i in range(1, len(rms)):
        if rms[i] < low_threshold:
            if not in_low_period:
                in_low_period = True
                low_start_time = times[i]  # Start of low volume period
            low_rms_values.append(rms[i])
        else:
            if in_low_period:
                # End of a low volume period
                if times[i] - low_start_time >= min_duration:
                    low_volume_periods.append((low_start_time, times[i], np.mean(low_rms_values)))  # Append period and avg RMS
                in_low_period = False
                low_rms_values = []

    # Step 4: Detect loudness spikes (more than high_multiplier * average of the low period)
    significant_changes = []
    
    for start_time, end_time, low_avg_rms in low_volume_periods:
        # Look for loudness spike after low volume period (3x the average)
        for i in range(len(rms)):
            if times[i] > end_time:  # After the low volume period
                if rms[i] > high_multiplier * low_avg_rms:
                    # Check if the spike lasts at least min_spike_duration
                    spike_start_time = times[i]
                    for j in range(i, len(rms)):
                        if times[j] > spike_start_time + min_spike_duration:
                            significant_changes.append((times[i], rms[i], times[j], rms[j]))  # Spike start and end times
                            break
                    break  # Only take the first spike after the low volume period

    return significant_changes

def filter_close_changes(significant_changes, min_time_gap=0.2, min_rms_diff=0.01):
    """
    Filters out consecutive changes that are too close in time or RMS value.

    Args:
        significant_changes (list): A list of tuples representing significant changes.
        min_time_gap (float): Minimum time gap between consecutive changes to consider them separate.
        min_rms_diff (float): Minimum difference in RMS values between consecutive changes.

    Returns:
        list: A list of filtered significant changes.
    """
    filtered_changes = []

    for i in range(len(significant_changes)):
        if i == 0:
            # Always keep the first change
            filtered_changes.append(significant_changes[i])
        else:
            prev_change = significant_changes[i - 1]
            current_change = significant_changes[i]
            
            time_gap = current_change[0] - prev_change[2]
            rms_diff = abs(current_change[1] - prev_change[3])

            if time_gap >= min_time_gap or rms_diff >= min_rms_diff:
                # If time gap or RMS difference is significant, keep the current change
                filtered_changes.append(current_change)

    return filtered_changes

def detect_quiet_segments(y, sr, quiet_threshold=0.002, min_duration=1.0, window_size=1024, hop_length=512):
    """
    Detects quiet segments in the audio where the RMS is below a threshold.

    Args:
        y (np.array): Audio signal.
        sr (int): Sampling rate of the audio.
        quiet_threshold (float): The RMS value below which the segment is considered quiet.
        min_duration (float): Minimum duration of the quiet segment in seconds.
        window_size (int): Window size for RMS calculation.
        hop_length (int): Hop length for RMS calculation.

    Returns:
        list: List of tuples with start time, end time, and average RMS value of quiet segments.
    """
    # Step 1: Calculate RMS values for the audio signal
    rms = librosa.feature.rms(y=y, frame_length=window_size, hop_length=hop_length)[0]
    times = librosa.frames_to_time(np.arange(len(rms)), sr=sr, hop_length=hop_length)
    
    # Step 2: Identify quiet segments (RMS < quiet_threshold)
    quiet_segments = []
    in_quiet_period = False
    quiet_start_time = None
    quiet_rms_values = []

    for i in range(1, len(rms)):
        if rms[i] < quiet_threshold:  # If the RMS is below the threshold, it's quiet
            if not in_quiet_period:
                in_quiet_period = True
                quiet_start_time = times[i - 1]  # Start of quiet period
            quiet_rms_values.append(rms[i])
        else:
            if in_quiet_period:
                # End of a quiet period, check if its duration is significant
                if times[i] - quiet_start_time >= min_duration:
                    # Add the quiet segment with average RMS as a regular float
                    quiet_segments.append((
                        float(quiet_start_time), 
                        float(times[i]), 
                        float(np.mean(quiet_rms_values))  # Convert RMS to standard float
                    ))
                in_quiet_period = False
                quiet_rms_values = []

    return quiet_segments

def compare_audio_context(features, significant_changes, context_window=5.0):
    """
    Compare parts of the audio before and after each significant change and calculate averages between changes.

    Args:
        features (dict): Extracted features from the audio analysis.
        significant_changes (list): List of significant changes.
        context_window (float): Time window (in seconds) before and after each change.

    Returns:
        tuple: 
            - List of changes with details on loudness and tempo.
            - List of averages for RMS and tempo between changes.
    """
    results = []
    segment_averages = []

    beat_times = np.array(features["BeatTimes"], dtype=float)
    rms = np.array(features["SpectralFlux"], dtype=float)  # Use spectral flux or RMS
    rms_times = librosa.frames_to_time(np.arange(len(rms)))

    # Add start and end times for full analysis
    change_times = [0] + [change[0] for change in significant_changes] + [rms_times[-1]]

    for i, change in enumerate(significant_changes):
        start_time, start_rms, end_time, end_rms = change

        # Get context windows
        pre_window_start = max(0, start_time - context_window)
        pre_window_end = start_time
        post_window_start = end_time
        post_window_end = end_time + context_window

        # Get average RMS in pre and post windows
        pre_rms_values = rms[(rms_times >= pre_window_start) & (rms_times < pre_window_end)]
        post_rms_values = rms[(rms_times >= post_window_start) & (rms_times < post_window_end)]

        pre_avg_rms = float(np.mean(pre_rms_values)) if len(pre_rms_values) > 0 else 0
        post_avg_rms = float(np.mean(post_rms_values)) if len(post_rms_values) > 0 else 0

        # Get tempo in pre and post windows
        pre_tempo = float(np.mean(beat_times[(beat_times >= pre_window_start) & (beat_times < pre_window_end)]))
        post_tempo = float(np.mean(beat_times[(beat_times >= post_window_start) & (beat_times < post_window_end)]))

        # Determine the change
        louder = post_avg_rms > pre_avg_rms
        faster = post_tempo > pre_tempo

        results.append({
            "Change": {
                "Start": float(start_time),
                "End": float(end_time),
                "StartRMS": float(start_rms),
                "EndRMS": float(end_rms)
            },
            "Louder": louder,
            "Faster": faster,
            "PreAvgRMS": pre_avg_rms,
            "PostAvgRMS": post_avg_rms,
            "PreTempo": pre_tempo,
            "PostTempo": post_tempo
        })

    # Calculate average RMS and tempo for each segment between changes
    for i in range(len(change_times) - 1):
        segment_start = change_times[i]
        segment_end = change_times[i + 1]

        # Get RMS and tempo values within the segment
        segment_rms_values = rms[(rms_times >= segment_start) & (rms_times < segment_end)]
        segment_beat_values = beat_times[(beat_times >= segment_start) & (beat_times < segment_end)]

        avg_rms = float(np.mean(segment_rms_values)) if len(segment_rms_values) > 0 else 0
        avg_tempo = float(np.mean(segment_beat_values)) if len(segment_beat_values) > 0 else 0

        segment_averages.append({
            "SegmentStart": segment_start,
            "SegmentEnd": segment_end,
            "AvgRMS": avg_rms,
            "AvgTempo": avg_tempo
        })

    return results, segment_averages

def analyze_audio(audio_file_path):
    """
    Analyzes the audio file and extracts relevant features.

    Args:
        audio_file_path (str): Path to the audio file.

    Returns:
        dict: A dictionary containing the extracted features.
    """
    try:
        # Load the audio file
        y, sr = librosa.load(audio_file_path, sr=None)

        # Emphasize higher frequencies for better detection
        y = librosa.effects.preemphasis(y)

        # Extract onsets
        onset_env = librosa.onset.onset_strength(y=y, sr=sr)

        # Extract tempo and beats
        tempo, beats = librosa.beat.beat_track(onset_envelope=onset_env, sr=sr)

        # Cluster onsets for tempo sections
        num_clusters = int(tempo / 60)
        tempo_changes = librosa.segment.agglomerative(onset_env, num_clusters)
        tempo_change_times = librosa.frames_to_time(tempo_changes, sr=sr)

        # Convert beat frames to times
        beat_times = librosa.frames_to_time(beats, sr=sr)

        # Extract beat strengths to add intensity variation
        beat_strengths = onset_env[beats]

        # Calculate loudness 
        S, phase = librosa.magphase(librosa.stft(y))
        rms = librosa.feature.rms(S=S)[0] 
        loudness_times = librosa.times_like(rms)

        # Find peaks in loudness
        loudness_peaks = librosa.util.peak_pick(rms, 
                                               pre_max=5, post_max=5, 
                                               pre_avg=5, post_avg=5, 
                                               delta=0.1, wait=10)
        loudness_peak_times = loudness_times[loudness_peaks]

        # Calculate spectral centroid
        spectral_centroid = librosa.feature.spectral_centroid(S=S)[0]
        spectral_centroid_times = librosa.times_like(spectral_centroid)

        # Calculate chroma features
        chroma = librosa.feature.chroma_cqt(y=y, sr=sr)
        chroma_times = librosa.times_like(chroma)

        # Calculate spectral flux
        S = np.abs(librosa.stft(y))
        spectral_flux = np.sqrt(np.sum(np.diff(S, axis=1) ** 2, axis=0))
        spectral_flux = np.pad(spectral_flux, (1, 0), 'constant')  # Align with other features

        # Call the detect_significant_loudness_changes function
        significant_changes = detect_significant_loudness_changes(y, sr)

        # Apply the refined filtering function
        filtered_significant_changes = filter_close_changes(significant_changes)

        # Convert numpy float32 to regular float for JSON serialization
        significant_changes = [(float(start), float(rms_start), float(end), float(rms_end)) for start, rms_start, end, rms_end in filtered_significant_changes]

        # Detect very quiet segments
        quiet_segments = detect_quiet_segments(y, sr, quiet_threshold=0.003, min_duration=0.5)

        # Create a dictionary to store the extracted features
        features = {
            "Tempo": float(tempo),
            "BeatTimes": beat_times.tolist(),
            "TempoChanges": tempo_changes.tolist(),
            "TempoChangeTimes": tempo_change_times.tolist(),
            "SignificantChanges": significant_changes,
            "QuietSegments": quiet_segments,
            "SpectralFlux": spectral_flux.tolist(),
            "BeatStrengths": beat_strengths.tolist(),
            "LoudnessPeakTimes": loudness_peak_times.tolist(),
            "SpectralCentroid": spectral_centroid.tolist(),
            "SpectralCentroidTimes": spectral_centroid_times.tolist(),
            "Chroma": chroma.tolist(),
            "ChromaTimes": chroma_times.tolist() 
        }

        # Compare audio context around significant changes
        significant_change_data, segment_elements_data = compare_audio_context(features, filtered_significant_changes)

        # Add the comparison data to the features dictionary
        features["SignificantChangeData"] = significant_change_data
        features["SegmentAverages"] = segment_elements_data

        return features

    except Exception as e:
        print(f"Error analyzing audio: {e}")
        return None

if __name__ == "__main__":
    # Set up command line argument parsing
    parser = argparse.ArgumentParser(description='Analyze an audio file and extract features.')
    parser.add_argument('audio_file_path', type=str, help='Path to the audio file to analyze.')
    parser.add_argument('save_path', type=str, help='Path to save the extracted features as a JSON file.')
    
    # Parse arguments
    args = parser.parse_args()

    # Call the analyze_audio function with the provided audio file path
    features = analyze_audio(args.audio_file_path)

    if features:
        # Save the extracted features to a JSON file
        with open(args.save_path, "w") as f:
            json.dump(features, f, indent=4)
        print(f"Audio features saved to: {args.save_path}")