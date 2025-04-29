namespace AudioFile;

public static class PitchShiftHelper
{
    public static float GetPitchFactor(float octaves = 0, float tones = 0, float semitones = 0, float cents = 0)
    {
        return MathF.Pow(2, (octaves * 12 + tones * 2 + semitones + cents / 100) / 12f);
    }

    public static float GetPitchFactorFromOctaves(float octaves) => GetPitchFactor(octaves: octaves);

    public static float GetPitchFactorFromTones(float tones) => GetPitchFactor(tones: tones);

    public static float GetPitchFactorFromSemitones(float semitones) => GetPitchFactor(semitones: semitones);

    public static float GetPitchFactorFromCents(float cents) => GetPitchFactor(cents: cents);
}