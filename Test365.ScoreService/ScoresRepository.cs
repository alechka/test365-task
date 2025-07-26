using Test365.Common;

namespace Test365.ScoreService;

/// <summary>
/// In memory repository for scores. In real world this should be a database
/// </summary>
public class ScoresRepository : IScoresRepository
{
    readonly Lock _lock = new ();
    
    private List<Score> Scores { get; } = new();

    /// <summary>
    /// Saves new score
    /// </summary>
    /// <param name="score"></param>
    public void Save(Score score)
    {
        lock (_lock)
        {
            Scores.Add(score);
        }
    }
    
    /// <summary>
    /// Lists scores
    /// </summary>
    public List<Score> List(ListFilter filter)
    {
        lock (_lock)
        {
            var query = Scores.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Team)) query = query.Where(s=> s.Team1.Contains(filter.Team) || s.Team2.Contains(filter.Team));
            if (!string.IsNullOrWhiteSpace(filter.Sport)) query = query.Where(s=> s.Sport.Contains(filter.Sport));
            if (filter.MinDate != null) query = query.Where(s=> s.Date > filter.MinDate);
            if (filter.MaxDate != null) query = query.Where(s=> s.Date > filter.MaxDate);
            return query.Take(filter.Take).ToList();
        }
    }
}