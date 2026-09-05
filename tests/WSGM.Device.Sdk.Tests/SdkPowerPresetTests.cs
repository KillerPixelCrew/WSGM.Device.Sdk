using System.Text.Json;
using WSGM.Device.Sdk.Capabilities;

namespace WSGM.Device.Tests;

public sealed class SdkPowerPresetTests
{
    private static CapabilityDescriptor Limit(CapabilityRole role) => new()
    {
        CapabilityId = role.ToString(),
        Role = role,
        ValueKind = CapabilityValueKind.Integer,
        Persistence = CapabilityPersistence.Volatile,
        Display = new() { Key = DisplayKey.SustainedPowerLimit },
        SupportsRead = true,
        SupportsWrite = true,
        Unit = CapabilityUnit.Watt,
        Minimum = 8,
        Maximum = 37,
        Step = 1,
    };

    private static CapabilityDescriptor[] Pair(params DevicePowerPreset[] presets) =>
        [Limit(CapabilityRole.PowerSustainedLimit) with { PowerPresets = presets }, Limit(CapabilityRole.PowerSlowLimit)];

    [Fact]
    public void EmptyPresetsPreserveExistingDescriptors() => Assert.True(DevicePowerPreset.TryValidate(Pair(), out _));

    [Fact]
    public void ValidTargetsRoundTripWithTheDescriptorSet()
    {
        var preset = new DevicePowerPreset("battery", "Super Battery", 8, 9, DevicePowerMode.BetterBattery);
        CapabilityDescriptorSet set = new() { Generation = 1, CycleGeneration = 1, Descriptors = Pair(preset) };
        Assert.True(DevicePowerPreset.TryValidate(set.Descriptors, out _));
        string json = JsonSerializer.Serialize(set);
        var read = JsonSerializer.Deserialize<CapabilityDescriptorSet>(json)!;
        Assert.Equal(preset, Assert.Single(read.Descriptors[0].PowerPresets));
    }

    [Theory]
    [InlineData("custom", "Valid", 8, 9, 0)]
    [InlineData("bad id", "Valid", 8, 9, 0)]
    [InlineData("valid", "bad\nlabel", 8, 9, 0)]
    [InlineData("valid", "Valid", 7, 9, 0)]
    [InlineData("valid", "Valid", 9, 8, 0)]
    [InlineData("valid", "Valid", 8, 38, 0)]
    [InlineData("valid", "Valid", 8, 9, 99)]
    public void InvalidPresetsAreRejected(string id, string name, int sustained, int slow, int mode) =>
        Assert.False(DevicePowerPreset.TryValidate(Pair(new DevicePowerPreset(id, name, sustained, slow, (DevicePowerMode)mode)), out _));

    [Fact]
    public void MissingAmbiguousReadonlyOrSteppedPartnersAreRejected()
    {
        var pair = Pair(new DevicePowerPreset("battery", "Battery", 8, 9, DevicePowerMode.BetterBattery));
        Assert.False(DevicePowerPreset.TryValidate([pair[0]], out _));
        Assert.False(DevicePowerPreset.TryValidate([.. pair, pair[1]], out _));
        Assert.False(DevicePowerPreset.TryValidate([pair[0], pair[1] with { SupportsWrite = false }], out _));
        Assert.False(DevicePowerPreset.TryValidate([pair[0], pair[1] with { Step = 2 }], out _));
        Assert.False(DevicePowerPreset.TryValidate([pair[0] with { InstanceId = "second" }, pair[1]], out _));
    }

    [Fact]
    public void DuplicateAndExcessivePresetsAreRejected()
    {
        var preset = new DevicePowerPreset("battery", "Battery", 8, 9, DevicePowerMode.BetterBattery);
        Assert.False(DevicePowerPreset.TryValidate(Pair(preset, preset), out _));
        Assert.False(DevicePowerPreset.TryValidate(Pair(Enumerable.Range(0, 17).Select(i => preset with { Id = $"p{i}" }).ToArray()), out _));
    }
}
