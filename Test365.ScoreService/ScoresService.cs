using Test365.Common;

namespace Test365.ScoreService;

internal class ScoresService(IScoresRepository repository, IDuplicateCheckingService duplicateCheckingService)
{
    public async Task<string> SaveAsync(Score score, CancellationToken ctx = default)
    {
        var id = score.GetIdWithoutTime();
        if (!await duplicateCheckingService.LockAsync(id))
        {
            //todo write logs
            return id;
        }
        
        repository.Save(score);
        return score.GetId();
    }
    
    public Task<List<Score>> ListAsync(ListFilter filter, CancellationToken ctx = default) => Task.FromResult(repository.List(filter));
    
}