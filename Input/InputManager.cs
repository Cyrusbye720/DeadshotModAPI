using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DeadshotModAPI;

/// <summary>
/// Provides keyboard input listening and event dispatch for Deadshot mods.
/// </summary>
public static class Input
{
    private static readonly Dictionary<Key, Action> _keyActions = new();

    /// <summary>
    /// Registers a callback to be invoked when the specified key is pressed.
    /// </summary>
    /// <param name="key">The key to listen for.</param>
    /// <param name="method">The callback to invoke when the key is pressed.</param>
    public static void OnKeyPressed(Key key, Action method)
    {
        if (method == null)
        {
            return;
        }

        if (_keyActions.TryGetValue(key, out Action existing))
        {
            _keyActions[key] = existing + method;
        }
        else
        {
            _keyActions[key] = method;
        }
    }

    /// <summary>
    /// Unregisters a previously registered callback from a key.
    /// </summary>
    /// <param name="key">The key being monitored.</param>
    /// <param name="method">The callback to remove.</param>
    public static void RemoveKeyPressed(Key key, Action method)
    {
        if (method == null)
        {
            return;
        }

        if (_keyActions.TryGetValue(key, out Action existing))
        {
            Action updated = existing - method;
            if (updated == null)
            {
                _keyActions.Remove(key);
            }
            else
            {
                _keyActions[key] = updated;
            }
        }
    }

    internal static void TriggerKey(Key key)
    {
        if (_keyActions.TryGetValue(key, out Action action) && action != null)
        {
            Delegate[] delegates;
            try
            {
                delegates = action.GetInvocationList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting invocation list for key {key}: {ex}");
                return;
            }

            foreach (Delegate handler in delegates)
            {
                if (handler is Action callback)
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error executing callback for key {key}: {ex}");
                    }
                }
            }
        }
    }

    public static string ToCamelCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return char.ToLowerInvariant(text[0]) + text.Substring(1);
    }

    private static bool IsPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        try
        {
            string keyName = $"{key.ToString().ToCamelCase()}Key";
            PropertyInfo property = typeof(Keyboard).GetProperty(keyName);

            if (property == null)
            {
                return false;
            }

            var keyControl = property.GetValue(Keyboard.current) as KeyControl;
            return keyControl?.wasPressedThisFrame ?? false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error checking key state for {key}: {ex}");
            return false;
        }
    }

    internal static void CheckKeys()
    {
        try
        {
            if (Keyboard.current == null)
            {
                return;
            }

            // Snapshot keys to allow modifying bindings during callback execution
            Key[] keys = _keyActions.Keys.ToArray();
            foreach (Key key in keys.Where(IsPressed))
            {
                TriggerKey(key);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error checking keys: {ex}");
        }
    }
}