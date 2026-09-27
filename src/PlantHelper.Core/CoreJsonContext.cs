using System.Text.Json.Serialization;
using PlantHelper.Core.Models;
using PlantHelper.Core.Services;

namespace PlantHelper.Core;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<NutrientDose>))]
[JsonSerializable(typeof(BackupManifest))]
[JsonSerializable(typeof(BackupData))]
internal partial class CoreJsonContext : JsonSerializerContext;
