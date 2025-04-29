using EnterTheCastle.Extensions.Logging;
using Microsoft.Xna.Framework;

namespace AudioFile;

public interface IBehavior : IDisposable
{
    //~IBehavior() => Dispose(false);
    
    void IDisposable.Dispose() => Dispose(true);

    public void Dispose(bool disposing)
    {
        try { UnloadContentIfNeeded(); }
        catch (Exception e) { Log.Error(e); }
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor - no shit? it's an interface
        GC.SuppressFinalize(this);
    }

    public float Priority => 0f;

    /// <summary>
    /// The object identity value.
    /// </summary>
    Guid ID { get; }

    /// <summary>
    /// The object identity value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The <see cref="Game">Game</see> to which this behavior is attached.
    /// </summary>
    Game Game => Game.Instance;

    /*/// <summary>
    /// The <see cref="Scene">Scene</see> to which this behavior is attached.
    /// </summary>
    Scene Scene { get; }*/

    /// <summary>
    /// The manager object for attached behaviors.
    /// </summary>
    BehaviorManager Behaviors { get; }

    /// <summary>
    /// True if the object should be updated and drawn, false otherwise.
    /// </summary>
    bool Active { get; set; }

    /// <summary>
    /// True if the call to Update(GameTime) should be skipped, false otherwise.
    /// </summary>
    bool Stasis { get; set; }

    /// <summary>
    /// True if the object has been initialized, false otherwise.
    /// </summary>
    bool Initialized { get; protected set; }

    /// <summary>
    /// True if the object has had it's content loaded, false otherwise.
    /// </summary>
    bool ContentLoaded { get; protected set; }

    /// <summary>
    /// Called when the object is first initialized, can be called again to reset the object.
    /// </summary>
    void Initialize() => Initialized = true;
    void InitializeIfNeeded() { if (!Initialized) Initialize(); }

    /// <summary>
    /// Loads any required assets for the behavior. This only gets called once per instance.
    /// </summary>
    void LoadContent() => ContentLoaded = true;
    void LoadContentIfNeeded() { if (!ContentLoaded) LoadContent(); }

    /// <summary>
    /// Unloads any required assets for the behavior. This isn't normally called unless a scene is tearing down.
    /// </summary>
    void UnloadContent() => ContentLoaded = false;
    void UnloadContentIfNeeded() { if (ContentLoaded) UnloadContent(); }

    /// <summary>
    /// Called once per logical frame to update the object state and perform any physics.
    /// Graphics code should not go here.
    /// </summary>
    /// <param name="gameTime">The XNA game time state object.</param>
    void Update(GameTime gameTime) { }

    /// <summary>
    /// Called once per logical frame after the main update at the end of the update lifecycle.
    /// Graphics code should not go here.
    /// </summary>
    /// <param name="gameTime">The XNA game time state object.</param>
    void LateUpdate(GameTime gameTime) { }

    /// <summary>
    /// Called once per visual frame to update the object graphics.
    /// Physics code should not go here.
    /// </summary>
    /// <param name="gameTime">The XNA game time state object.</param>
    void Draw(GameTime gameTime) { }

    /// <summary>
    /// Called once per visual frame to update the gui object graphics.
    /// Physics code should not go here.
    /// </summary>
    /// <param name="gameTime">The XNA game time state object.</param>
    void DrawGUI(GameTime gameTime) { }

    /// <summary>
    /// Called once per visual frame to after the graphics have been drawn.
    /// Physics code should not go here.
    /// </summary>
    void EndDraw() { }
}