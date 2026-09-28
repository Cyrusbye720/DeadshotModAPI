using System;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

namespace DeadshotModAPI.Tests.Tier2_BoundaryCornerCases;

public class InputBoundaryTests : IDisposable
{
    public InputBoundaryTests()
    {
        ReflectionHelper.ResetInputState();
        TestLoggerMock.ResetLogs();
    }

    public void Dispose()
    {
        ReflectionHelper.ResetInputState();
        TestLoggerMock.ResetLogs();
    }

    [Fact]
    public void Input_OnKeyPressed_NullAction_DoesNotThrowAndDoesNotCrashOnTrigger()
    {
        // Registering a null action should be handled gracefully or not crash TriggerKey
        var ex = Record.Exception(() =>
        {
            Input.OnKeyPressed(Key.Space, null!);
            ReflectionHelper.InvokeTriggerKey(Key.Space);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Input_RemoveKeyPressed_UnregisteredKey_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
        {
            Input.RemoveKeyPressed(Key.F1, () => { });
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Input_RemoveKeyPressed_NullAction_DoesNotThrow()
    {
        Input.OnKeyPressed(Key.F2, () => { });

        var ex = Record.Exception(() =>
        {
            Input.RemoveKeyPressed(Key.F2, null!);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Input_TriggerKey_UnregisteredKey_ExecutesNoOpWithoutException()
    {
        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.F12);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Input_TriggerKey_ThrowingAction_CatchesExceptionAndLogsError()
    {
        bool secondCallbackRan = false;

        Input.OnKeyPressed(Key.E, () => throw new InvalidOperationException("Explosion in key callback"));
        Input.OnKeyPressed(Key.E, () => secondCallbackRan = true);

        var ex = Record.Exception(() =>
        {
            ReflectionHelper.InvokeTriggerKey(Key.E);
        });

        Assert.Null(ex);
        Assert.True(secondCallbackRan, "Subsequent callbacks must still run even if earlier callback throws.");
        Assert.Contains(TestLoggerMock.LoggedErrors, err => err.Contains("Explosion in key callback"));
    }

    [Theory]
    [InlineData((Key)(-1))]
    [InlineData((Key)9999)]
    public void Input_OnKeyPressed_InvalidKeyEnum_RegistersAndTriggersDefensively(Key invalidKey)
    {
        bool called = false;
        Input.OnKeyPressed(invalidKey, () => called = true);

        ReflectionHelper.InvokeTriggerKey(invalidKey);

        Assert.True(called);
    }
}
