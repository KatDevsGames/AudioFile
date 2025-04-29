using EnterTheCastle.Extensions;
using Microsoft.Xna.Framework;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioFile;

public class Mp3Player(Game game) : BehaviorBase(game)
{
    private const int BUFFER_SIZE = 4096;
    
    private Mp3FileReader m_mp3FileReader;
    //private float[] m_mp3Samples = new float[BUFFER_SIZE];
    //private byte[] m_mp3Buffer = new byte[BUFFER_SIZE * sizeof(float)];

    //private BufferedWaveProvider m_waveProvider;
    private WasapiOut m_waveOut;
    
    public SITimeSpan CurrentTime
    {
        get => m_mp3FileReader.CurrentTime;
        set => m_mp3FileReader.CurrentTime = (TimeSpan)value;
    }
    
    public long Position
    {
        get => m_mp3FileReader.Position;
        set => m_mp3FileReader.Position = value;
    }

    public override void Update(GameTime gameTime)
    {
        try
        {
            //int bytesRead = m_mp3FileReader.Read(m_mp3Buffer);
            //m_waveProvider.AddSamples(m_mp3Buffer, 0, bytesRead);
        }
        catch (EndOfStreamException)
        {
            Game.SongPlaying = false;
            Game.SongTime = SITimeSpan.Zero;
        }

        base.Update(gameTime);
    }

    public void LoadFile(string path)
    {
        m_mp3FileReader = new(path);
    
        //m_waveProvider = new(m_mp3FileReader.WaveFormat) { DiscardOnBufferOverflow = true };
        m_waveOut = new(AudioClientShareMode.Shared, 10);
        m_waveOut.Init(m_mp3FileReader);
    }

    public void Play() => m_waveOut.Play();

    public void Stop() => m_waveOut.Stop();
}