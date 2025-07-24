namespace Test365.PublisherConsole;

public interface IRandomFromArray
{
    /// <summary>
    /// Gets random data from array
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    string Get(string[] list);
}