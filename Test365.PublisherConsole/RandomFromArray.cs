namespace Test365.PublisherConsole;

public class RandomFromArray : IRandomFromArray
{
    private readonly Random _random = new Random();
    
    /// <summary>
    /// Gets random data from array
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public string Get(string[] list)
    {
        return list[_random.Next(list.Length)];
    }
}