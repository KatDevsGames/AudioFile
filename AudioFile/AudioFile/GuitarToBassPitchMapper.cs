namespace AudioFile;

public static class GuitarToBassPitchMapper
{
    public enum GuitarString
    {
        E2 = 1, // Lowest guitar string
        A2,
        D3,
        G3,
        B3,
        E4 // Highest guitar string
    }

    public enum BassString
    {
        E1 = 1, // Lowest bass string
        A1,
        D2,
        G2 // Highest bass string
    }
        
    public static float MapGuitarToBassPitchFactor(GuitarString guitarString, BassString bassString)
    {
        int guitarMidi = GetMidiNoteForGuitarString(guitarString);
        int bassMidi = GetMidiNoteForBassString(bassString);
        int semitoneShift = bassMidi - guitarMidi;

        return PitchShiftHelper.GetPitchFactorFromSemitones(semitoneShift);
    }

    public static float MapBassToGuitarPitchFactor(BassString bassString, GuitarString guitarString)
    {
        return MapGuitarToBassPitchFactor(guitarString, bassString);
    }

    private static int GetMidiNoteForGuitarString(GuitarString guitarString) => guitarString switch
    {
        GuitarString.E2 => 40,
        GuitarString.A2 => 45,
        GuitarString.D3 => 50,
        GuitarString.G3 => 55,
        GuitarString.B3 => 59,
        GuitarString.E4 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(guitarString))
    };

    private static int GetMidiNoteForBassString(BassString bassString) => bassString switch
    {
        BassString.E1 => 28,
        BassString.A1 => 33,
        BassString.D2 => 38,
        BassString.G2 => 43,
        _ => throw new ArgumentOutOfRangeException(nameof(bassString))
    };
}