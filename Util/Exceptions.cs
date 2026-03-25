public class BFExecutionException : Exception
{
    public int StepsTaken { get; }
    public BFExecutionException(string message, int steps) : base(message)
    {
        StepsTaken = steps;
    }
}