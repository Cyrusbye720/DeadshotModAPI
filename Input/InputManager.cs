using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Globalization;

namespace DeadshotModAPI;

public static class Input
{
    private static readonly Dictionary<Key, Action> _keyActions = new();

    /// <summary>
    /// Registers a method to be called when the specified key is pressed.
    /// Multiple methods can be registered to the same key.
    /// </summary>
    /// <param name="key">The key to listen for.</param>
    /// <param name="method">The method to invoke when the key is pressed.</param>
    public static void OnKeyPressed(Key key, Action method)
    {
        if (_keyActions.TryGetValue(key, out var existing))
            _keyActions[key] = existing + method;
        else
            _keyActions[key] = method;
    }

    /// <summary>
    /// Invokes all methods registered to the specified key.
    /// This method is called internally by the input system when a key press is detected.
    /// </summary>
    /// <param name="key">The key whose registered methods should be invoked.</param>
    internal static void TriggerKey(Key key)
    {
        if (_keyActions.TryGetValue(key, out var action))
        {
            action?.Invoke();
        }
    }

    /// <summary>
    /// Converts the first character of a string to lowercase.
    /// This is used to convert PascalCase names into camelCase names.
    /// </summary>
    /// <param name="text">The string to convert.</param>
    /// <returns>
    /// The string with its first character converted to lowercase,
    /// or the original string if it is null, empty, or whitespace.
    /// </returns>
    public static string ToCamelCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return char.ToLowerInvariant(text[0]) + text.Substring(1);
    }

    /// <summary>
    /// Determines whether the specified key was pressed during the current frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    /// <c>true</c> if the key was pressed during the current frame;
    /// otherwise, <c>false</c>.
    /// </returns>
    private static bool IsPressed(Key key)
    {
        string keyName = $"{key.ToString().ToCamelCase()}Key";

        var property = typeof(Keyboard).GetProperty(keyName);

        if (property == null)
            return false;

        var keyControl = property.GetValue(Keyboard.current) as KeyControl;

        return keyControl?.wasPressedThisFrame ?? false;
    }

    /// <summary>
    /// Checks all keys currently registered with the input system
    /// and triggers their associated methods when pressed.
    /// </summary>
    internal static void CheckKeys()
    {
        foreach (var key in _keyActions.Keys)
        {
            if (IsPressed(key))
                TriggerKey(key);
        }
    }
}