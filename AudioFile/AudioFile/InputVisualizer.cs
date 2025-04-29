using System.Text;
using EnterTheCastle.Extensions;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using NAudio.Wave;

namespace AudioFile;

public class InputVisualizer : BehaviorBase
{
    private const float GAIN = 4.0f;

    private readonly float[] m_waveBuffer = new float[1280];
    
    private readonly WaveInEvent m_waveIn;
    private BufferedWaveProvider m_waveProvider;

    private readonly StringBuilder m_noteString = new();
    
    private readonly Note[] m_notes = new Note[128];
    
    public InputVisualizer(Game game) : base(game)
    {
        m_waveIn = new();
        m_waveIn.DeviceNumber = -1;
        m_waveIn.WaveFormat = new(Game.SAMPLE_RATE, 1);
        m_waveIn.BufferMilliseconds = 10;
        m_waveIn.DataAvailable += OnDataAvailable;
        m_waveIn.StartRecording();
        
        m_waveProvider = new(m_waveIn.WaveFormat) { DiscardOnBufferOverflow = true };
    }

    public override void Draw(GameTime gameTime)
    {
        float lastSample = float.NaN;
        for (int i = 0; i < m_waveBuffer.Length; i++)
        {
            float sample = m_waveBuffer[i];
            if (!float.IsNaN(lastSample))
                SpriteBatch.DrawLine(new(i - 1, (lastSample * Game.WINDOW_SIZE.Height) + Game.HALF_HEIGHT), new(i, (sample * Game.WINDOW_SIZE.Height) + Game.HALF_HEIGHT), Color.White, 1f);
            lastSample = sample;
        }
        
        if (m_noteString.Length > 0) SpriteBatch.DrawString(DebugFont, m_noteString, new(10, 10), Color.White);
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        Task.Run(() =>
        {
            bool locked = false;
            try
            {
                locked = Monitor.TryEnter(m_waveBuffer);
                if (!locked) return;
                int i;
                for (i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i + 0]);
                    int j = i / 2;
                    if (j >= m_waveBuffer.Length) return;
                    m_waveBuffer[j] = (sample / (float)short.MaxValue) * GAIN;
                }

                Span<Note> notes = stackalloc Note[128];
                NoteDetection.DetectNotes(m_waveBuffer, Game.SAMPLE_RATE, ref notes);
                for (i = 0; i < notes.Length; i++)
                {
                    Note note = notes[i];
                    if (note == Note.None) break;
                    if (i == 0) m_noteString.Clear();
                    m_notes[i] = note;
                    m_noteString.Append(note + " ");
                }

                m_notes[i] = Note.None;
            }
            finally
            {
                if (locked) Monitor.Exit(m_waveBuffer);
            }
        }).Forget();
    }
}