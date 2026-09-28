using System.Runtime.CompilerServices;
using DeadshotModAPI.Tests.TestHelpers;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DeadshotModAPI.Tests;

public static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        TestLoggerMock.Initialize();
    }
}
