using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Poss.Win.Automation.Common.Keys.Enums;
using Poss.Win.Automation.Common.Structs;
using Poss.Win.Automation.GlobalHotKeys.Structs;

namespace Poss.Win.Automation.GlobalHotKeys
{
    /// <summary>
    /// Worker that handles hotkey matching, re-trigger prevention, and registration.
    /// Receives input snapshots from the core; all logic runs off the hook thread.
    /// Thread-safe for concurrent Register/Unregister/Change/GetRegisteredHotkeys.
    /// </summary>
    internal sealed class GlobalHotKeys
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, (HotkeyCombination Combo, Func<Task> Action)> _registry = new ConcurrentDictionary<string, (HotkeyCombination, Func<Task>)>();
        private readonly Dictionary<string, HashSet<VirtualKey>> _active = new Dictionary<string, HashSet<VirtualKey>>();
        private int _autoIdCounter;

        internal async Task ProcessInputAsync(KeyStroke stroke, HashSet<VirtualKey> pressedKeys)
        {
            if (pressedKeys == null)
                return;

            var toInvoke = new List<(string Id, Func<Task> Action)>();

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (stroke.Action == KeyAction.Up)
                {
                    foreach (var id in _active.Keys.ToList())
                    {
                        if (_active[id].Any(k => HotkeyCombination.KeysMatch(k, stroke.Key)))
                            _active.Remove(id);
                    }

                    foreach (var kvp in _registry)
                    {
                        var id = kvp.Key;
                        var (combo, action) = kvp.Value;
                        if (!combo.HasUpTrigger || !combo.Matches(stroke, pressedKeys) || _active.ContainsKey(id))
                            continue;
                        _active[id] = combo.GetKeys();
                        toInvoke.Add((id, action));
                    }
                }
                else
                {
                    foreach (var kvp in _registry)
                    {
                        var id = kvp.Key;
                        var (combo, action) = kvp.Value;
                        if (combo.HasUpTrigger || !combo.Matches(stroke, pressedKeys) || _active.ContainsKey(id))
                            continue;
                        _active[id] = combo.GetKeys();
                        toInvoke.Add((id, action));
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            foreach (var (_, action) in toInvoke)
                _ = Task.Run(async () => await action().ConfigureAwait(false));
        }

        /// <summary>
        /// Registers a hotkey binding with the specified id.
        /// </summary>
        /// <param name="id">Unique identifier for the binding.</param>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="strokes">Key strokes that define the combination.</param>
        /// <returns>The binding id.</returns>
        public string Register(string id, Func<Task> action, params KeyStroke[] strokes)
        {
            var combo = strokes == null || strokes.Length == 0
                ? new HotkeyCombination()
                : new HotkeyCombination(strokes);
            _registry[id] = (combo, action ?? throw new ArgumentNullException(nameof(action)));
            return id;
        }

        /// <summary>
        /// Registers a hotkey binding with the specified id.
        /// </summary>
        /// <param name="id">Unique identifier for the binding.</param>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="keysString">Key combination string. Format: "Ctrl + A Up", "Shift + LButton".</param>
        /// <returns>The binding id.</returns>
        public string Register(string id, Func<Task> action, string keysString)
        {
            var combo = HotkeyCombination.Parse(keysString);
            _registry[id] = (combo, action ?? throw new ArgumentNullException(nameof(action)));
            return id;
        }

        /// <summary>
        /// Registers a hotkey binding with an auto-generated id.
        /// </summary>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="strokes">Key strokes that define the combination.</param>
        /// <returns>The auto-generated binding id.</returns>
        public string Register(Func<Task> action, params KeyStroke[] strokes)
        {
            var id = "hk_" + (uint)Interlocked.Increment(ref _autoIdCounter);
            return Register(id, action, strokes);
        }

        /// <summary>
        /// Registers a hotkey binding with an auto-generated id.
        /// </summary>
        /// <param name="action">Async callback invoked when the hotkey is triggered.</param>
        /// <param name="keysString">Key combination string.</param>
        /// <returns>The auto-generated binding id.</returns>
        public string Register(Func<Task> action, string keysString)
        {
            var id = "hk_" + (uint)Interlocked.Increment(ref _autoIdCounter);
            return Register(id, action, keysString);
        }

        /// <summary>
        /// Registers a hotkey binding asynchronously. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(string id, Func<Task> action, params KeyStroke[] strokes)
        {
            Register(id, action, strokes);
            return Task.FromResult(id);
        }

        /// <summary>
        /// Registers a hotkey binding asynchronously with a key string. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(string id, Func<Task> action, string keysString)
        {
            Register(id, action, keysString);
            return Task.FromResult(id);
        }

        /// <summary>
        /// Registers a hotkey binding asynchronously with auto-generated id. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(Func<Task> action, params KeyStroke[] strokes)
        {
            var id = Register(action, strokes);
            return Task.FromResult(id);
        }

        /// <summary>
        /// Registers a hotkey binding asynchronously with auto-generated id and key string. Returns immediately with the id.
        /// </summary>
        public Task<string> RegisterAsync(Func<Task> action, string keysString)
        {
            var id = Register(action, keysString);
            return Task.FromResult(id);
        }

        /// <summary>
        /// Unregisters a hotkey binding by id.
        /// </summary>
        /// <param name="id">The binding id to remove.</param>
        public void Unregister(string id)
        {
            _registry.TryRemove(id, out _);
            _semaphore.Wait();
            try
            {
                _active.Remove(id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Unregisters a hotkey binding by id asynchronously.
        /// </summary>
        public async Task UnregisterAsync(string id)
        {
            _registry.TryRemove(id, out _);
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _active.Remove(id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Changes the key combination for an existing binding.
        /// </summary>
        /// <param name="id">The binding id.</param>
        /// <param name="newStrokes">New key strokes for the combination.</param>
        public void Change(string id, params KeyStroke[] newStrokes)
        {
            if (_registry.TryGetValue(id, out var entry))
                _registry[id] = (new HotkeyCombination(newStrokes), entry.Action);
        }

        /// <summary>
        /// Changes the key combination for an existing binding using a key string.
        /// </summary>
        public void Change(string id, string newKeysString)
        {
            if (_registry.TryGetValue(id, out var entry))
                _registry[id] = (HotkeyCombination.Parse(newKeysString), entry.Action);
        }

        /// <summary>
        /// Changes the action callback for an existing binding.
        /// </summary>
        public void Change(string id, Func<Task> newAction)
        {
            if (_registry.TryGetValue(id, out var entry))
                _registry[id] = (entry.Combo, newAction ?? throw new ArgumentNullException(nameof(newAction)));
        }

        /// <summary>
        /// Returns a copy of all registered hotkey bindings (id and combination only). Does not include action callbacks.
        /// </summary>
        public IReadOnlyList<HotkeyBinding> GetRegisteredHotkeys()
        {
            return _registry
                .Select(kvp => new HotkeyBinding(kvp.Key, kvp.Value.Combo))
                .ToList();
        }

        /// <summary>
        /// Returns a copy of all registered hotkey bindings asynchronously.
        /// </summary>
        public Task<IReadOnlyList<HotkeyBinding>> GetRegisteredHotkeysAsync()
        {
            return Task.FromResult(GetRegisteredHotkeys());
        }
    }
}
