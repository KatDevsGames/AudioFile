using NAudio.Wave;

namespace AudioFile;

public class sandbox
{
    private void test(object sender)
    {
        

        
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        float max = 0;
        WaveBuffer buffer = new WaveBuffer(e.Buffer);
        // interpret as 32 bit floating point audio
        for (int index = 0; index < e.BytesRecorded / 4; index++)
        {
            float sample = buffer.FloatBuffer[index];

            sample = MathF.Abs(sample);
            // is this the max value?
            if (sample > max) max = sample;
        }
    }
}