using Test365.Common;

namespace Test365.Tests;

/// <summary>
/// Tests for getting id from scores
/// </summary>
public class TestScoresIdGet
{
    [Fact]
    public void DatesNonEqual()
    {
        var score1 = new Score("s1", new DateTime(2025, 07, 24, 10, 0, 0), "t1", "t2", 10, 10);
        var score2 = new Score("s1", new DateTime(2025, 07, 25, 10, 0, 0), "t1", "t2", 10, 10);
        Assert.NotEqual(score1.GetId(), score2.GetId());
    }
    
    [Fact]
    public void DatesEqualTeamsMixed()
    {
        var dt = new DateTime(2025, 07, 24, 10, 0, 0);
        var score1 = new Score("s1",  dt , "t1", "t2", 10, 10);
        var score2 = new Score("s1", dt, "t2", "t1", 10, 10);
        Assert.Equal(score1.GetId(), score2.GetId());
    }
    
    [Fact]
    public void DatesInTwoHours()
    {
        var dt = new DateTime(2025, 07, 24, 10, 0, 0);
        var dt2 = dt.AddMinutes(90);
        var score1 = new Score("s1",  dt , "t1", "t2", 10, 10);
        var score2 = new Score("s1", dt2, "t1", "t2", 10, 10);
        Assert.Equal(score1.GetId(), score2.GetId());
    }
}