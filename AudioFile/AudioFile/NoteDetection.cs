using System.Numerics;
using Accord.Math;

namespace AudioFile;

public static class NoteDetection
{
    public static void DetectNotes(float[] samples, int sampleRate, ref Span<Note> detectedNotes)
    {
        // Apply FFT and detect multiple frequencies
        List<(float frequency, float magnitude)> frequencies = DetectFrequencies(samples, sampleRate);

        detectedNotes.Clear();

        // Convert frequencies to musical notes
        int j = 0;
        frequencies.Sort((a, b) => -a.magnitude.CompareTo(b.magnitude));
        for (int i = 0; i < frequencies.Count; i++)
        {
            float frequency = frequencies[i].frequency;
            if (frequency > 0)
            {
                if (!IsSignificantChange(frequency)) break;
                Note detectedNote = FrequencyToNote(frequency);
                if (detectedNote != Note.None)
                    detectedNotes[j++] = detectedNote;
            }

            break;
        }
    }

    private static void ApplyHanningWindow(float[] samples)
    {
        int length = samples.Length;
        for (int i = 0; i < length; i++)
            samples[i] *= 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (length - 1)));
    }

    private static float m_lastDetectedFrequency = 0;

    private static bool IsSignificantChange(float frequency)
    {
        float difference = Math.Abs(frequency - m_lastDetectedFrequency);
        (float distanceUp, float distanceDown) = GetNoteDistance(frequency);
        if (difference > MathF.Min(MathF.Abs(distanceUp), MathF.Abs(distanceDown))) // Only accept a change of more than 2 Hz
        {
            m_lastDetectedFrequency = frequency;
            return true;
        }

        return false;
    }

    private static readonly float SEMITONE_RATIO = MathF.Pow(2f, 1 / 12f); // Twelfth root of 2

    public static (float distanceUp, float distanceDown) GetNoteDistance(float frequency)
    {
        // Calculate the closest note's MIDI number (A4 = 440 Hz is MIDI 69)
        int closestMidi = (int)MathF.Round(69 + 12 * MathF.Log2(frequency / 440f));

        // Frequency of the closest note
        float closestNoteFreq = 440f * MathF.Pow(SEMITONE_RATIO, closestMidi - 69);

        // Calculate frequency of the next higher and lower notes
        float nextNoteUp = closestNoteFreq * SEMITONE_RATIO;
        float nextNoteDown = closestNoteFreq / SEMITONE_RATIO;

        // Calculate distances
        float distanceUp = nextNoteUp - frequency;
        float distanceDown = frequency - nextNoteDown;

        return (distanceUp, distanceDown);
    }

    private static List<(float FrequencyToNote, float magnitude)> DetectFrequencies(float[] samples, int sampleRate)
    {
        int fftSize = NextPowerOfTwo(samples.Length);
        Complex[] fftBuffer = new Complex[fftSize];

        // Copy the sample data into the real part of the Complex array
        for (int i = 0; i < samples.Length; i++)
        {
            fftBuffer[i] = new Complex(samples[i], 0);
        }

        // Apply FFT
        FourierTransform.FFT(fftBuffer, FourierTransform.Direction.Forward);

        List<(float frequency, float magnitude)> detectedFrequencies = new();
        float binSize = sampleRate / (float)fftSize;

        // Peak detection: find the most significant peaks
        for (int i = 1; i < fftBuffer.Length / 2; i++) // Only consider the positive frequencies
        {
            float magnitude = (float)fftBuffer[i].Magnitude;
            if (magnitude > 0.0025f) // You can adjust this threshold
            {
                detectedFrequencies.Add((i * binSize, magnitude));
            }
        }

        return detectedFrequencies;
    }

    public static Note FrequencyToNote(float frequency)
    {
        // Electric Guitar typically ranges from E2 (82.41 Hz) to E6 (1318.51 Hz)
        // We will clamp frequencies outside the common guitar range

        if (frequency is < 82.41f or > 1318.51f)
            return Note.None; // Return an unknown note if it's outside guitar range

        // Apply a slight calibration shift (e.g., for tuning errors)
        float calibratedFrequency = frequency * 1;//.002f; // Slight tuning correction

        // Find the MIDI note from the frequency
        int midiNote = (int)Math.Round(69 + 12 * Math.Log2(calibratedFrequency / 440.0));

        // Ensure that the note is within the valid range for electric guitar (E2 to E6)
        midiNote = Math.Clamp(midiNote, 40, 88); // MIDI range: 40 (E2) to 88 (E6)

        // Return the corresponding note in the extended enum
        return (Note)midiNote; // Mapping MIDI notes within the guitar range
    }

    private static int NextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value) power *= 2;
        return power;
    }
}