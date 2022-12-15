#if NET6_0_OR_GREATER
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Inedo.DependencyScan
{
    [JsonSerializable(typeof(DependentPackage))]
    [JsonSerializable(typeof(IEnumerable<DependentPackage>))]
    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    internal sealed partial class JsonContext : JsonSerializerContext
    {
    }
}
#endif
