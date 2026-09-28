using System;
using Xunit;

namespace DeadshotModAPI.Tests.Tier1_FeatureCoverage;

public class DeadshotModFeatureTests
{
    private class ConcreteTestMod : IDeadshotMod
    {
        public string Name => "Test Mod";
        public string Description => "Test Mod Description";
        public string Creator => "Author";
        public string Version => "1.0.0";

        public bool IsLoaded { get; private set; }

        public void Load()
        {
            IsLoaded = true;
        }
    }

    [Fact]
    public void DeadshotMod_Interface_DefinesExpectedContract()
    {
        var iface = typeof(IDeadshotMod);
        Assert.True(iface.IsInterface);

        var nameProp = iface.GetProperty("Name");
        var descProp = iface.GetProperty("Description");
        var creatorProp = iface.GetProperty("Creator");
        var verProp = iface.GetProperty("Version");
        var loadMethod = iface.GetMethod("Load");

        Assert.NotNull(nameProp);
        Assert.NotNull(descProp);
        Assert.NotNull(creatorProp);
        Assert.NotNull(verProp);
        Assert.NotNull(loadMethod);

        Assert.True(nameProp.CanRead);
        Assert.False(nameProp.CanWrite);
    }

    [Fact]
    public void DeadshotMod_Implementation_ReturnsMetadataCorrectly()
    {
        IDeadshotMod mod = new ConcreteTestMod();

        Assert.Equal("Test Mod", mod.Name);
        Assert.Equal("Test Mod Description", mod.Description);
        Assert.Equal("Author", mod.Creator);
        Assert.Equal("1.0.0", mod.Version);
    }

    [Fact]
    public void DeadshotMod_Load_ExecutesModSpecificLogic()
    {
        var mod = new ConcreteTestMod();
        Assert.False(mod.IsLoaded);

        mod.Load();

        Assert.True(mod.IsLoaded);
    }

    [Fact]
    public void DeadshotMod_Polymorphism_AssignableToIDeadshotMod()
    {
        Assert.True(typeof(IDeadshotMod).IsAssignableFrom(typeof(ConcreteTestMod)));
    }

    [Fact]
    public void DeadshotMod_MultipleInstances_MaintainIndependentState()
    {
        var mod1 = new ConcreteTestMod();
        var mod2 = new ConcreteTestMod();

        mod1.Load();

        Assert.True(mod1.IsLoaded);
        Assert.False(mod2.IsLoaded);
    }
}
