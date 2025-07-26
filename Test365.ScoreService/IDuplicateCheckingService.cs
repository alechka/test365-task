namespace Test365.ScoreService;

public interface IDuplicateCheckingService
{
    /// <summary>
    /// Locks the key and check if it is not existant
    /// </summary>
    ///<returns>true if successful, false if not</returns>
    Task<bool> LockAsync(string key);
}