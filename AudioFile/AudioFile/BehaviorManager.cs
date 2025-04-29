using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EnterTheCastle.Extensions;
using EnterTheCastle.Extensions.Logging;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AudioFile;

[Serializable, JsonConverter(typeof(Converter))]
public class BehaviorManager : IBehavior, IEnumerable<IBehavior>
{
    public Guid ID { get; } = Guid.NewGuid();

    public string Name => nameof(BehaviorManager);

#pragma warning disable CS8618
    private class BehaviorEntry
    {
        public readonly Guid ID = Guid.NewGuid();
        public readonly int Priority;
        public readonly BehaviorSource Behavior;
        public delegate bool BehaviorSource([MaybeNullWhen(false)] out IBehavior behavior);
        
        public BehaviorEntry(int priority, IBehavior behavior)
        {
            Priority = priority;
            Behavior = (out IBehavior b) =>
            {
                b = behavior;
                return true;
            };
        }
        
        public BehaviorEntry(int priority, WeakReference<IBehavior> behavior)
        {
            Priority = priority;
            Behavior = behavior.TryGetTarget;
        }

        public override string ToString()
        {
            if (!Behavior(out IBehavior? behavior)) return $"[Expired Reference][{Priority}]";
            return (behavior.ToString() ?? string.Empty) + '[' + Priority + '/' + ((behavior.ContentLoaded) ? "Loaded" : "Unloaded") + ']';
        }
    }
#pragma warning restore CS8618

    private readonly SortedDictionary<int, List<BehaviorEntry>> m_behaviorsPriority = new(ReverseComparer<int>.Instance);
    private readonly Dictionary<string, BehaviorEntry> m_behaviorsName = new();

    public BehaviorManager(params IEnumerable<IBehavior> behaviors) => AddBehaviors(behaviors);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<IBehavior> GetEnumerator() => Behaviors.GetEnumerator();

    public Func<IBehavior, bool>? UpdateFilter { get; set; }
    public Func<IBehavior, bool>? DrawFilter { get; set; }

    BehaviorManager IBehavior.Behaviors => this;

    public bool Active
    {
        get => true;
        set { }
    }
    
    public bool Stasis
    {
        get => false;
        set { }
    }
    
    public bool Initialized
    {
        get => true;
        set { }
    }
    
    public bool ContentLoaded
    {
        get => true;
        set { }
    }

    public IEnumerable<IBehavior> Behaviors
    {
        get
        {
            foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
            foreach (BehaviorEntry entry in entries)
            {
                if (!entry.Behavior(out IBehavior? behavior))
                    continue;
                yield return behavior;
            }
        }
    }

    private IEnumerable<BehaviorEntry> AllBehaviorEntries
    {
        get
        {
            foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
            foreach (BehaviorEntry entry in entries)
                yield return entry;
        }
    }
    
    public bool TryFind<T>(out IEnumerable<T> behavior) where T : IBehavior
    {
        behavior = Behaviors.OfType<T>();
        return behavior.Any();
    }

    public bool TryFind<T>(string name, out T behavior) where T : IBehavior
    {
        if (m_behaviorsName.TryGetValue(name, out BehaviorEntry? entry) && entry.Behavior(out IBehavior? b))
        {
            behavior = (T)b;
            return true;
        }

        behavior = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehaviors(params IEnumerable<IBehavior?> behaviors)
    {
        foreach (IBehavior? behavior in behaviors)
            if (behavior != null)
                AddBehavior(behavior);
    }

    /// <remarks></remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehaviors(params IEnumerable<WeakReference<IBehavior>?> behaviors)
    {
        foreach (WeakReference<IBehavior>? behavior in behaviors)
            if (behavior != null)
                AddBehavior(behavior);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehavior(IBehavior behavior)
        => AddBehavior(behavior.Name, (int)behavior.Priority, behavior);

    public void AddBehavior(WeakReference<IBehavior> behavior)
    {
        if (!behavior.TryGetTarget(out IBehavior? target)) return;
        AddBehavior(target.Name, (int)target.Priority, behavior);
    }

    public void AddBehaviors(int priority, params IEnumerable<IBehavior?> behaviors)
    {
        foreach (IBehavior? behavior in behaviors)
            if (behavior != null)
                AddBehavior(priority, behavior);
    }

    public void AddBehaviors(int priority, params IEnumerable<WeakReference<IBehavior>?> behaviors)
    {
        foreach (WeakReference<IBehavior>? behavior in behaviors)
            if (behavior != null)
                AddBehavior(priority, behavior);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehavior(int priority, IBehavior behavior)
        => AddBehavior(behavior.Name, priority, behavior);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehavior(int priority, WeakReference<IBehavior> behavior)
    {
        if (!behavior.TryGetTarget(out IBehavior? target)) return;
        AddBehavior(target.Name, priority, behavior);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBehavior(string name, IBehavior behavior)
        => AddBehavior(name, (int)behavior.Priority, behavior);

    public void AddBehavior(string name, WeakReference<IBehavior> behavior)
    {
        if (!behavior.TryGetTarget(out IBehavior? target)) return;
        AddBehavior(name, (int)target.Priority, behavior);
    }

    public void AddBehavior(string name, int priority, IBehavior behavior)
    {
        BehaviorEntry entry = new(priority, behavior);

        m_behaviorsName.Add(name, entry);
        if (!m_behaviorsPriority.TryGetValue(priority, out List<BehaviorEntry>? behaviors))
            m_behaviorsPriority[priority] = behaviors = [];

        behavior.InitializeIfNeeded();
        behaviors.Add(entry);
    }

    public void AddBehavior(string name, int priority, WeakReference<IBehavior> behavior)
    {
        if (!behavior.TryGetTarget(out IBehavior? target)) return;
        BehaviorEntry entry = new(priority, behavior);

        m_behaviorsName.Add(name, entry);
        if (!m_behaviorsPriority.TryGetValue(priority, out List<BehaviorEntry>? behaviors))
            m_behaviorsPriority[priority] = behaviors = [];

        target.InitializeIfNeeded();
        behaviors.Add(entry);
    }

    public void RemoveBehavior(IBehavior entry)
    {
        foreach (var behaviorEntry in m_behaviorsName)
        {
            if (!Equals(behaviorEntry.Value.Behavior, entry)) continue;
            RemoveBehavior(behaviorEntry.Key);
            return;
        }
    }

    public void RemoveBehavior(WeakReference<IBehavior> behavior)
    {
        if (!behavior.TryGetTarget(out IBehavior? target)) return;
        RemoveBehavior(target);
    }

    public void RemoveBehaviors(params IEnumerable<IBehavior?> behaviors)
    {
        foreach (IBehavior? behavior in behaviors)
            if (behavior != null)
                RemoveBehavior(behavior);
    }

    public void RemoveBehaviors(params IEnumerable<WeakReference<IBehavior>?> behaviors)
    {
        foreach (WeakReference<IBehavior>? behavior in behaviors)
            if (behavior != null)
                RemoveBehavior(behavior);
    }

    private void RemoveBehavior(BehaviorEntry entry)
    {
        foreach (var behaviorEntry in m_behaviorsName)
        {
            if (!Equals(behaviorEntry.Value, entry)) continue;
            RemoveBehavior(behaviorEntry.Key);
            return;
        }
    }

    public void RemoveBehavior(string name)
    {
        BehaviorEntry entry = m_behaviorsName[name];
        m_behaviorsName.Remove(name);
        m_behaviorsPriority[entry.Priority].Remove(entry);

        if (entry.Behavior(out IBehavior? behavior))
            behavior.UnloadContentIfNeeded();
    }

    public void RemoveExcept(IBehavior? behavior)
    {
        foreach (BehaviorEntry entry in AllBehaviorEntries.ToArray())
            if ((!entry.Behavior(out IBehavior? b)) || (!ReferenceEquals(b, behavior)))
                RemoveBehavior(entry);
    }

    public void Clear()
    {
        foreach (BehaviorEntry behavior in AllBehaviorEntries)
        {
            if (behavior.Behavior(out IBehavior? b))
                b.UnloadContentIfNeeded();
        }
        m_behaviorsPriority.Clear();
        m_behaviorsName.Clear();
    }

    public void CleanupExpired()
    {
        foreach (BehaviorEntry behavior in AllBehaviorEntries.ToArray())
            if (!behavior.Behavior(out _))
                RemoveBehavior(behavior);
    }

    public void LoadContent()
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        foreach (BehaviorEntry entry in entries)
            if (entry.Behavior(out IBehavior? behavior))
                behavior.LoadContentIfNeeded();
        ContentLoaded = true;
    }

    public void UnloadContent()
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
            foreach (BehaviorEntry entry in entries)
                if (entry.Behavior(out IBehavior? behavior))
                    behavior.UnloadContentIfNeeded();
        ContentLoaded = false;
    }

    public void Update(GameTime gameTime)
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        {
            try
            {
                foreach (BehaviorEntry entry in entries)
                {
                    if (!entry.Behavior(out IBehavior? behavior)) continue;
                    
                    if (!(UpdateFilter?.Invoke(behavior) ?? true))
                        continue;

                    behavior.LoadContentIfNeeded();
                    if (behavior is { Active: true, Stasis: false })
                        behavior.Update(gameTime);
                }
            }
            catch (Exception e) { Log.Error(e); }
        }
    }

    public void LateUpdate(GameTime gameTime)
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        {
            try
            {
                foreach (BehaviorEntry entry in entries)
                {
                    if (!entry.Behavior(out IBehavior? behavior)) continue;
                    
                    if (!(UpdateFilter?.Invoke(behavior) ?? true))
                        continue;
                    
                    if (!behavior.ContentLoaded) continue;
                    if (behavior.Active)
                        behavior.LateUpdate(gameTime);
                }
            }
            catch (Exception e) { Log.Error(e); }
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        {
            try
            {
                foreach (BehaviorEntry entry in entries)
                {
                    if (!entry.Behavior(out IBehavior? behavior)) continue;
                    
                    if (!(DrawFilter?.Invoke(behavior) ?? true))
                        continue;
                    
                    if (!behavior.ContentLoaded) continue;
                    if (behavior.Active)
                        behavior.Draw(gameTime);
                }
            }
            catch (Exception e) { Log.Error(e); }
        }
    }

    public void DrawGUI(GameTime gameTime)
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        {
            try
            {
                foreach (BehaviorEntry entry in entries)
                {
                    if (!entry.Behavior(out IBehavior? behavior)) continue;
                    
                    if (!(DrawFilter?.Invoke(behavior) ?? true))
                        continue;
                    
                    if (!behavior.ContentLoaded) continue;
                    if (behavior.Active)
                        behavior.DrawGUI(gameTime);
                }
            }
            catch (Exception e) { Log.Error(e); }
        }
    }

    public void EndDraw()
    {
        foreach (List<BehaviorEntry> entries in m_behaviorsPriority.Values)
        {
            try
            {
                foreach (BehaviorEntry entry in entries)
                {
                    if (!entry.Behavior(out IBehavior? behavior)) continue;
                    
                    if (!(DrawFilter?.Invoke(behavior) ?? true))
                        continue;
                    
                    if (!behavior.ContentLoaded) continue;
                    if (behavior.Active)
                        behavior.EndDraw();
                }
            }
            catch (Exception e) { Log.Error(e); }
        }
    }

    private class Converter : JsonConverter<BehaviorManager>
    {
        public override void WriteJson(JsonWriter writer, BehaviorManager? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JArray jArray = [];
            foreach (var prioritySet in value.m_behaviorsPriority)
            {
                JObject j = new()
                {
                    ["priority"] = prioritySet.Key,
                    ["behaviors"] = JArray.FromObject(prioritySet.Value.Select(e => e.Behavior(out IBehavior? b) ? b : null).OfType<IBehavior>())
                };
                jArray.Add(j);
            }
            serializer.Serialize(writer, jArray);
        }

        public override BehaviorManager ReadJson(JsonReader reader, Type objectType, BehaviorManager? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}