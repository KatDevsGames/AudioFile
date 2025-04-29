using AlphaTab.Importer;
using AlphaTab.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Color = Microsoft.Xna.Framework.Color;

namespace AudioFile;

public class TabVisualizer : BehaviorBase
{
    private Score? m_score;

    public float Time { get; set; }
    
    public float BPM { get; set; }

    private float m_offset = HALF_SCREEN;
    
    private Texture2D[]? m_textures;
    private readonly SpriteFont m_fretFont = Game.Instance.Content.Load<SpriteFont>("Fonts/FretMarkers");

    private const int TOP_LINE_OFFSET = 50;
    private const int LINE_SPACING = 50;
    private const int HALF_LINE_SPACING = LINE_SPACING / 2;
    private const int FRET_LABEL_INDENT = 5;
    private const int QUARTER_NOTE_TICKS = 960;
    private const int QUARTER_NOTE_WIDTH = 100;
    private const int NOTE_HEIGHT = 40;
    private const int HALF_NOTE_HEIGHT = NOTE_HEIGHT / 2;
    private const int STAFF_HEIGHT = LINE_SPACING * 6;
    private const int TOTAL_HEIGHT = (LINE_SPACING * 5) + (TOP_LINE_OFFSET * 2);

    private static readonly int HALF_SCREEN = Game.WINDOW_SIZE.Width / 2;
    private const int CENTER_BAR_THICKNESS = 10;
    private static readonly int CENTER_BAR_X = HALF_SCREEN - (CENTER_BAR_THICKNESS / 2);
    private static readonly Color CENTER_BAR_COLOR = Color.Red.WithAlpha(0.3f);
    
    private static readonly Color[] FRET_COLORS =
    [
        Color.Goldenrod,
        Color.DarkMagenta,
        Color.Blue,
        Color.Green,
        Color.DarkOrange,
        Color.DeepPink,
        Color.DarkCyan,
        Color.DarkRed,
        Color.DarkGreen,
        Color.DarkBlue
    ];
    
    private readonly Dictionary<int, Color> m_fretColors = new();

    public TabVisualizer(Game game) : base(game) => Stasis = true;

    private Color GetFretColor(int fret)
    {
        if (m_fretColors.TryGetValue(fret, out Color color)) return color;
        return m_fretColors[fret] = FRET_COLORS[m_fretColors.Count % FRET_COLORS.Length];
    }

    public void DrawFretMarker(AlphaTab.Model.Note note, float barOffset, float beatOffset, float beatWidth)
    {
        float lineSpacing = STAFF_HEIGHT / 6f;
        
        float xPos = beatOffset + barOffset;
        float yPos = TOP_LINE_OFFSET + ((6f - (float)note.String) * lineSpacing) - HALF_NOTE_HEIGHT;

        RectangleF rect = new(xPos, yPos, beatWidth, NOTE_HEIGHT);
        Game.Instance.SpriteBatch.FillRectangle(rect, GetFretColor((int)note.Fret), 0.1f);
        Game.Instance.SpriteBatch.DrawRectangle(rect, Color.Black, 1f, 0.2f);

        string text = ((int)note.Fret).ToString();
        Vector2 stringSize = m_fretFont.MeasureString(text);
        //Vector2 stringPosition = new Vector2(rect.Position.X + (rect.Width / 2f) - (stringSize.X / 2f), rect.Position.Y + (rect.Height / 2f) - (stringSize.Y / 2f));
        Vector2 stringPosition = new Vector2(rect.Position.X + FRET_LABEL_INDENT, rect.Position.Y + (rect.Height / 2f) - (stringSize.Y / 2f));
        //Game.Instance.SpriteBatch.DrawString(m_fretFont, text, stringPosition, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.3f);
        DrawOutlined(m_fretFont, text, stringPosition, Color.White, Color.Black, 1, 0.3f);
    }

    public float DrawBar(Bar bar, float barOffset)
    {
        float width = EstimateBarWidth(bar);
        float lineSpacing = STAFF_HEIGHT / 6f;
        for (int i = 0; i < 6; i++)
        {
            float yPos = TOP_LINE_OFFSET + (i * lineSpacing);
            Game.Instance.SpriteBatch.DrawLine(new Vector2(barOffset, yPos), new Vector2(barOffset + width, yPos), Color.Black, 1, 0f);
        }

        float vertOrigin = TOP_LINE_OFFSET - HALF_LINE_SPACING;
        Game.Instance.SpriteBatch.DrawLine(new Vector2(barOffset, vertOrigin), new Vector2(barOffset, TOTAL_HEIGHT - vertOrigin), Color.Black, 1, 0f);
        return barOffset + width;
    }

    public static float EstimateBarWidth(Bar bar)
    {
        return QUARTER_NOTE_WIDTH * 4f;
        float totalTicks = 0;
        foreach (Beat beat in bar.Voices[0].Beats)
            totalTicks += (float)beat.PlaybackDuration;
        return (totalTicks / QUARTER_NOTE_TICKS) * QUARTER_NOTE_WIDTH;
    }

    public static float EstimateTrackWidth(Track track)
    {
        float totalWidth = 0;
        Bar? lastBar = GetLastBarWithNotes(track);
        foreach (Staff staff in track.Staves)
        {
            float staffWidth = 0;
            foreach (Bar bar in staff.Bars)
            {
                staffWidth += EstimateBarWidth(bar);
                if (bar == lastBar) break;
            }
            totalWidth = Math.Max(totalWidth, staffWidth);
        }
        return totalWidth;
    }

    public static Bar? GetLastBarWithNotes(Track track)
    {
        foreach (Staff staff in track.Staves)
        {
            for (int i = staff.Bars.Count - 1; i >= 0; i--)
            {
                Bar bar = staff.Bars[i];
                foreach (Beat beat in bar.Voices[0].Beats)
                    if (beat.Notes.Count > 0)
                        return bar;
            }
        }
        return null;
    }

    public Texture2D[] LoadTexture(Score score)
    {
        RenderTarget2D[] textures = new RenderTarget2D[score.Tracks.Count];
        for (int i = 0; i < score.Tracks.Count; i++)
        {
            Track track = score.Tracks[i];
            RenderTarget2D target = textures[i] = new(Game.Instance.GraphicsDevice, (int)EstimateTrackWidth(track), TOTAL_HEIGHT);
            // ReSharper disable once ConvertToUsingDeclaration
            Game.Instance.GraphicsDevice.SetRenderTarget(target);
            Game.Instance.GraphicsDevice.Clear(Color.CornflowerBlue);
            Game.Instance.SpriteBatch.Begin(
                samplerState: SamplerState.AnisotropicClamp,
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend);
            {
                foreach (Staff staff in track.Staves)
                {
                    float barOffset = 0;
                    foreach (Bar bar in staff.Bars)
                    {
                        float nextBarOffset = DrawBar(bar, barOffset);
                        float beatOffset = 0;
                        foreach (Beat beat in bar.Voices[0].Beats) //todo other voices
                        {
                            float beatWidth = ((float)beat.PlaybackDuration / QUARTER_NOTE_TICKS) * QUARTER_NOTE_WIDTH;
                            foreach (AlphaTab.Model.Note note in beat.Notes)
                            {
                                DrawFretMarker(note, barOffset, beatOffset, beatWidth);
                            }
                            beatOffset += beatWidth;
                        }
                        barOffset = nextBarOffset;
                    }
                }
            }
            Game.Instance.SpriteBatch.End();
        }
        Game.Instance.GraphicsDevice.SetRenderTarget(null);
        return textures;
    }

    public void ReadTabs(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        m_score = ScoreLoader.LoadScoreFromBytes(fileData);
        BPM = (float)m_score.Tempo;
        m_textures = LoadTexture(m_score);
    }
    
    public override void Update(GameTime gameTime)
    {
        float beatsPerSecond = BPM / 60f;
        float timeInBeats = Time * beatsPerSecond;
        m_offset = HALF_SCREEN - (timeInBeats * QUARTER_NOTE_WIDTH);
        Time += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime)
    {
        if (m_textures == null) return;
        for (int i = 0; i < 1; i++)
            Game.Instance.SpriteBatch.Draw(m_textures[i], new Vector2(m_offset, i * 400), Color.White);
        Game.Instance.SpriteBatch.DrawLine(CENTER_BAR_X, 0, CENTER_BAR_X, 400, CENTER_BAR_COLOR, CENTER_BAR_THICKNESS, 0.1f);
        DrawOutlined(m_fretFont, "Time: " + Time.ToString("0.00"), new Vector2(10, 410), Color.White, Color.Black, 1, 0.3f);
        DrawOutlined(m_fretFont, "BPM: " + BPM.ToString("0.0"), new Vector2(10, 370), Color.White, Color.Black, 1, 0.3f);
    }

    public void DrawOutlined(SpriteFont font, string text, Vector2 position, Color color, Color outlineColor, float outlineThickness, float drawDepth)
    {
        Game.Instance.SpriteBatch.DrawString(font, text, new Vector2(position.X - outlineThickness, position.Y), outlineColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, drawDepth);
        Game.Instance.SpriteBatch.DrawString(font, text, new Vector2(position.X + outlineThickness, position.Y), outlineColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, drawDepth);
        Game.Instance.SpriteBatch.DrawString(font, text, new Vector2(position.X, position.Y - outlineThickness), outlineColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, drawDepth);
        Game.Instance.SpriteBatch.DrawString(font, text, new Vector2(position.X, position.Y + outlineThickness), outlineColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, drawDepth);
        Game.Instance.SpriteBatch.DrawString(font, text, new Vector2(position.X, position.Y), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, drawDepth + 0.01f);
    }
}