namespace Test365.ScoreService;

public interface IDuplicateCheckingService
{
    /// <summary>
    /// Locks the key in reddis, if successfully
    /// </summary>
    ///<returns>true if successful, false if not</returns>
    Task<bool> LockAsync(string key);
}