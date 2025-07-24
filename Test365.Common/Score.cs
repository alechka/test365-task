namespace Test365.Common;

public record Score(string Sport, DateTime Date, string Team1, string Team2, int Score1, int Score2)
{
    public string GetId()
    {
        //making two hour range
        var dt = new DateTime(Date.Year, Date.Month, Date.Day, 2 * (Date.Hour / 2), 0,0);
        return $"{GetIdWithoutTime()}:{dt:MM/dd/yyyy:HH}";
    }
    
    /// <summary>
    /// Gets the identity without time stamp
    /// </summary>
    /// <returns></returns>
    public string GetIdWithoutTime()
    {
        var sportSubstring = string.CompareOrdinal(Team1, Team2) < 0 ? Team1 + Team2 : Team2 + Team1;
        return $"{Sport}:{sportSubstring}";
    }
    
}