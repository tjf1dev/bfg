public class ExecutionException : Exception
{
    public int StepsTaken { get; }
    public ExecutionException(string message, int steps) : base(message)
    {
        StepsTaken = steps;
    }
}
public class InvalidInstruction : Exception
{
    public char Instruction { get; }
    public InvalidInstruction(string message, char instruction) : base(message)
    {
        Instruction = instruction;
    }
}