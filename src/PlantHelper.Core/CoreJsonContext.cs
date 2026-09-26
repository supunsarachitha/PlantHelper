using System.Text.Json.Serialization;
using PlantHelper.Core.Models;

namespace PlantHelper.Core;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<NutrientDose>))]
internal partial class CoreJsonContext : JsonSerializerContext;
