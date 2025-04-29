using EnterTheCastle.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using NAudio.Wave;
using KeyboardEventArgs = MonoGame.Extended.Input.InputListeners.KeyboardEventArgs;
using MouseEventArgs = MonoGame.Extended.Input.InputListeners.MouseEventArgs;

namespace AudioFile;

public class Game : Microsoft.Xna.Framework.Game
{
    public static Game Instance { get; private set; } = null!;

    public const int FPS = 100;
    public const int SAMPLES_PER_FRAME = SAMPLE_RATE / FPS;
    public const int BYTES_PER_FRAME = SAMPLES_PER_FRAME * sizeof(float);
    
    private readonly GraphicsDeviceManager m_graphics;
    public new GraphicsDevice GraphicsDevice => m_graphics.GraphicsDevice;
    public readonly SpriteBatch SpriteBatch;
    public static readonly Size WINDOW_SIZE = new(1280, 800);

    public const int SAMPLE_RATE = 48000;
    public const int HALF_HEIGHT = 400;
    
    private readonly TabVisualizer m_tabVisualizer;
    private readonly InputVisualizer m_inputVisualizer;
    private readonly Mp3Player m_mp3Player;

    private readonly KeyboardListener m_keyboardListener = new(new() { RepeatPress = true });
    private readonly GamePadListener m_gamePadListener = new();
    private readonly MouseListener m_mouseListener = new();

    private const float SEEK_TIME_SHORT = 5;
    private const float SEEK_TIME_MEDIUM = 5;
    private const float SEEK_TIME_LONG = 5;

    private const float BPM_ADJUSTMENT = 0.1f;

    public BehaviorManager Behaviors { get; } = new();

    public bool SongPlaying
    {
        get;
        set
        {
            if (field != value)
            {
                if (value) m_mp3Player.Play();
                else m_mp3Player.Stop();
            }

            m_tabVisualizer.Stasis = !value;
            m_mp3Player.Active = value;
            field = value;
        }
    }

    public SITimeSpan SongTime
    {
        get;
        set
        {
            field = value;
            m_tabVisualizer.Time = (float)value;
            m_mp3Player.CurrentTime = value;
        }
    }

    public SITimeSpan AudioDelay;
    public SITimeSpan VideoDelay;

    public Game()
    {
        Instance = this;

        //Content Loader
        Content.RootDirectory = "Content";

        //Misc
        IsMouseVisible = true;

        //Physics Tick Rate
        TargetElapsedTime = TimeSpan.FromSeconds(1f/FPS);
        InactiveSleepTime = TimeSpan.Zero;

        Console.WriteLine("Input devices: ");
        for (int n = -1; n < WaveInEvent.DeviceCount; n++)
        {
            WaveInCapabilities caps = WaveInEvent.GetCapabilities(n);
            Console.WriteLine($"{n}: {caps.ProductName} : {caps.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48M08)}");
        }

        Console.WriteLine();
        Console.WriteLine("Output devices: ");
        for (int n = -1; n < WaveOut.DeviceCount; n++)
        {
            WaveOutCapabilities caps = WaveOut.GetCapabilities(n);
            Console.WriteLine($"{n}: {caps.ProductName} : {caps.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48M08)}");
        }
        
        m_graphics = new(this);
        m_graphics.GraphicsProfile = GraphicsProfile.Reach;
        m_graphics.PreferredBackBufferFormat = SurfaceFormat.ColorSRgb;
        m_graphics.PreferredBackBufferWidth = WINDOW_SIZE.Width;
        m_graphics.PreferredBackBufferHeight = WINDOW_SIZE.Height;
        m_graphics.ApplyChanges();
        
        SpriteBatch = new(GraphicsDevice);

        //don't create behaviors before this point, because they need the GraphicsDevice device and Instance fields to be set up
        m_tabVisualizer = new(this); 
        m_inputVisualizer = new(this);
        m_mp3Player = new(this);

        //SmbPitchShiftingSampleProvider pitch = new(m_waveProvider.ToSampleProvider());
        //pitch.PitchFactor = PitchShiftHelper.GetPitchFactorFromOctaves(-0.5f);
        
        m_mouseListener.MouseClicked += OnMouseClicked;
        m_keyboardListener.KeyPressed += OnKeyPressed;
    }
    
    protected override void Initialize()
    {
        base.Initialize();

        Behaviors.AddBehaviors(m_tabVisualizer, m_inputVisualizer, m_mp3Player);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        m_tabVisualizer.ReadTabs(@"C:\Users\KatDevsGames\Downloads\The Legend Of Zelda Ocarina Of Time - Gerudo Valley (ver 5 by HCkev).gp5");
        m_mp3Player.LoadFile(@"C:\Users\KatDevsGames\Downloads\68 Gerudo Valley.mp3");
        //m_tabVisualizer.ReadTabs(@"C:\Users\KatDevsGames\Downloads\Snake Man Stage (Mega Man 3).gp5");
        //m_mp3Player.LoadFile(@"C:\Users\KatDevsGames\Downloads\10 Snake Man Stage.mp3");
    }

    private void OnMouseClicked(object? _, MouseEventArgs e)
    {
        
    }
    
    private void OnKeyPressed(object? _, KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case Keys.Left:
                if (e.Modifiers.HasFlag(KeyboardModifiers.Shift))
                    SongTime -= SEEK_TIME_LONG;
                else if (e.Modifiers.HasFlag(KeyboardModifiers.Control))
                    SongTime -= SEEK_TIME_SHORT;
                else
                    SongTime -= SEEK_TIME_MEDIUM;
                break;
            case Keys.Right:
                if (e.Modifiers.HasFlag(KeyboardModifiers.Shift))
                    SongTime += SEEK_TIME_LONG;
                else if (e.Modifiers.HasFlag(KeyboardModifiers.Control))
                    SongTime += SEEK_TIME_SHORT;
                else
                    SongTime += SEEK_TIME_MEDIUM;
                break;
            case Keys.Add:
                m_tabVisualizer.BPM += BPM_ADJUSTMENT;
                break;
            case Keys.Subtract:
                m_tabVisualizer.BPM -= BPM_ADJUSTMENT;
                break;
            case Keys.Space:
                SongPlaying = !SongPlaying;
                break;
            case Keys.Escape:
                Exit();
                break;
            case Keys.F11:
                m_graphics.ToggleFullScreen();
                break;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Behaviors.Update(gameTime);
        
        m_keyboardListener.Update(gameTime);
        m_gamePadListener.Update(gameTime);
        m_mouseListener.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        
        SpriteBatch.Begin(
            samplerState: SamplerState.PointWrap,
            sortMode: SpriteSortMode.FrontToBack,
            blendState: BlendState.AlphaBlend);

        Behaviors.Draw(gameTime);

        SpriteBatch.End();
    }
}