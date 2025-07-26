using System.Collections.Concurrent;
using Test365.Common;

namespace Test365.ApiService;

public class ListRequestHandler
{
    public int NumberOfResponses = 0;
    public ConcurrentBag<Score> Responses = new ();
    public TaskCompletionSource<IEnumerable<Score>> TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
}