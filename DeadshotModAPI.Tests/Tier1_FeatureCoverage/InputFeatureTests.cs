using System;
using System.Collections.Generic;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class InputFeatureTests : IDisposable
{
    public InputFeatureTests()
    {
        ReflectionHelper.ResetInputState();
    }

    public void Dispose()
    {
        ReflectionHelper.ResetInputState();
    }

    [Fact]
    public void OnKeyPressed_RegistersSingleAction_SuccessfullyInvokedViaTriggerKey()
    {
        // Arrange
        bool invoked = false;
        Action callback = () => invoked = true;

        // Act
        Input.OnKeyPressed(Key.F1, callback);
        ReflectionHelper.InvokeTriggerKey(Key.F1);

        // Assert
        Assert.True(invoked, "Registered callback on Key.F1 was expected to be invoked.");
    }

    [Fact]
    public void OnKeyPressed_MultipleActionsSameKey_AllInvokedInOrder()
    {
        // Arrange
        var executionLog = new List<string>();
        Action action1 = () => executionLog.Add("first");
        Action action2 = () => executionLog.Add("second");
        Action action3 = () => executionLog.Add("third");

        // Act
        Input.OnKeyPressed(Key.F2, action1);
        Input.OnKeyPressed(Key.F2, action2);
        Input.OnKeyPressed(Key.F2, action3);
        ReflectionHelper.InvokeTriggerKey(Key.F2);

        // Assert
        Assert.Equal(3, executionLog.Count);
        Assert.Equal(new[] { "first", "second", "third" }, executionLog);
    }

    [Fact]
    public void RemoveKeyPressed_RemovesAction_NoLongerInvoked()
    {
        // Arrange
        int callCount = 0;
        Action callback = () => callCount++;

        Input.OnKeyPressed(Key.F3, callback);
        ReflectionHelper.InvokeTriggerKey(Key.F3);
        Assert.Equal(1, callCount);

        // Act
        Input.RemoveKeyPressed(Key.F3, callback);
        ReflectionHelper.InvokeTriggerKey(Key.F3);

        // Assert
        Assert.Equal(1, callCount);
    }

    [Theory]
    [InlineData("Space", "space")]
    [InlineData("Digit0", "digit0")]
    [InlineData("LeftShift", "leftShift")]
    [InlineData("F12", "f12")]
    [InlineData("A", "a")]
    [InlineData("NumpadEnter", "numpadEnter")]
    [InlineData("camelCase", "camelCase")]
    public void ToCamelCase_StandardPascalCase_ConvertsFirstCharToLower(string input, string expected)
    {
        // Act
        string result = input.ToCamelCase();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void KeyEnum_ContainsAllStandardKeyCategories()
    {
        // Verify key enum values exist and can be cast
        var values = Enum.GetValues<Key>();

        Assert.True(values.Length >= 90, $"Expected at least 90 key definitions, found {values.Length}.");
        Assert.Contains(Key.A, values);
        Assert.Contains(Key.Z, values);
        Assert.Contains(Key.Digit0, values);
        Assert.Contains(Key.F1, values);
        Assert.Contains(Key.F12, values);
        Assert.Contains(Key.Space, values);
        Assert.Contains(Key.Escape, values);
        Assert.Contains(Key.LeftShift, values);
        Assert.Contains(Key.RightAlt, values);
        Assert.Contains(Key.Numpad0, values);
    }

    [Fact]
    public void OnKeyPressed_MultipleDifferentKeys_AreIndependent()
    {
        // Arrange
        bool key1Invoked = false;
        bool key2Invoked = false;

        Input.OnKeyPressed(Key.F4, () => key1Invoked = true);
        Input.OnKeyPressed(Key.F5, () => key2Invoked = true);

        // Act
        ReflectionHelper.InvokeTriggerKey(Key.F4);

        // Assert
        Assert.True(key1Invoked);
        Assert.False(key2Invoked);
    }
}
