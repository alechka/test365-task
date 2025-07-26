using JetBrains.Annotations;

namespace Test365.Common;

/// <summary>
/// Filter to list scores
/// </summary>
[UsedImplicitly]
public record ListFilter
{
    public string? Team { get; [UsedImplicitly] set; }
    
    public string? Sport { get; [UsedImplicitly] set; }
    
    public DateTime? MinDate { get; [UsedImplicitly] set; }
    
    public DateTime? MaxDate { get; [UsedImplicitly] set; }
    
    public int Take { get; set; }
}