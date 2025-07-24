using Test365.Common;

namespace Test365.ScoreService;

public interface IScoresRepository
{
    /// <summary>
    /// Saves new score
    /// </summary>
    /// <param name="score"></param>
    void Save(Score score);

    /// <summary>
    /// Lists scores
    /// </summary>
    List<Score> List(ListFilter filter);
}