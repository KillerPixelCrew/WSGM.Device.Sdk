namespace WSGM.Device.Sdk;

/// <summary>Identifies the exact public plugin API supported by this build.</summary>
public static class DeviceApi
{
    /// <summary>Exact version required by WSGM, Device Lab, and every plugin.</summary>
    /// <remarks>
    /// Version 2 added the overlay section vocabulary: <c>CapabilityDescriptorSet.Sections</c> and
    /// the descriptor's <c>CategoryId</c>/<c>SortOrder</c> placement fields.
    /// </remarks>
    public const int Version = 2;
}
