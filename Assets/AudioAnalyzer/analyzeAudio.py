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
        Tempo, beats = librosa.beat.beat_track(onset_envelope=onset_env, sr=sr)

        # Cluster onsets for tempo sections
        num_clusters = int(Tempo / 60)
        tempo_changes = librosa.segment.agglomerative(onset_env, num_clusters)
        tempo_change_times = librosa.frames_to_time(tempo_changes, sr=sr)

        # Convert beat frames to times
        BeatTimes = librosa.frames_to_time(beats, sr=sr)

        # Extract beat strengths to add intensity variation
        BeatStrengths = onset_env[beats]

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
            "Tempo": float(Tempo),
            "BeatTimes": BeatTimes.tolist(),
            "tempo_changes": tempo_changes.tolist(),
            "tempo_change_times": tempo_change_times.tolist(),
            "significant_changes": significant_changes,
            "quiet_segments": quiet_segments,
            "spectral_flux": spectral_flux.tolist(),
            "BeatStrengths": BeatStrengths.tolist(),
            "loudness_peak_times": loudness_peak_times.tolist(),
            "spectral_centroid": spectral_centroid.tolist(),
            "spectral_centroid_times": spectral_centroid_times.tolist(),
            "chroma": chroma.tolist(),
            "chroma_times": chroma_times.tolist() 
        }

        return features

    except Exception as e:
        print(f"Error analyzing audio: {e}")
        return None

if __name__ == "__main__":
    # audio_file_path = "C:\\PythonProject\\Intocable - Fuerte No Soy.mp3"
    audio_file_path = "C:\\PythonProject\\Passenger - Let Her Go.mp3"
    features = analyze_audio(audio_file_path)

    if features:
        # Specify the full path to save the JSON file
        save_path = "C:\\PythonProject\\audio_features.json" 

        # Save features to a JSON file
        with open(save_path, "w") as f:
            json.dump(features, f, indent=4)
        print(f"Audio features saved to: {save_path}")