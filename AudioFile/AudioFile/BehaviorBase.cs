using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AudioFile;

public abstract class BehaviorBase : GameComponent, IBehavior
{
    public Guid ID { get; } = Guid.NewGuid();
    
    public new Game Game { get; }

    protected SpriteBatch SpriteBatch => Game.SpriteBatch;

    public string Name { get; }

    public bool Active { get; set; } = true;

    public bool Stasis { get; set; } = false;

    public bool Initialized { get; set; }

    public bool ContentLoaded { get; set; }

    [field: MaybeNull]
    public BehaviorManager Behaviors => field ??= new();

    protected static SpriteFont? DebugFont => field ??= Game.Instance?.Content.Load<SpriteFont>("Fonts/DebugSmall");

    protected BehaviorBase(Game game) : base(game)
    {
        Game = game;
        Name = GetType().Name;
    }

    public override void Initialize() => Initialized = true;

    public virtual void LoadContent() => ContentLoaded = true;
    
    public virtual void UnloadContent() => ContentLoaded = false;

    public virtual void Draw(GameTime gameTime) { }
}