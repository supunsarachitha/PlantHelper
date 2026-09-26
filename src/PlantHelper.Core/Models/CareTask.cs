namespace PlantHelper.Core.Models;

/// <summary>A computed (not stored) watering or feeding task.</summary>
public sealed record CareTask(Plant Plant, CareTaskKind Kind, DateTime DueDate, DateTime? LastDone, int DaysOverdue)
{
    public bool IsOverdue => DaysOverdue > 0;
    public bool IsDueToday => DaysOverdue == 0;
}
