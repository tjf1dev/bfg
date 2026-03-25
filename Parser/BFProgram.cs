using System.Net.Mime;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;

class BFProgram
{
    private readonly StringBuilder output = new();
    public string GetOutput() => output.ToString();
    private readonly byte[] memory = new byte[30000];
    private Dictionary<int, int> jumpTable = new(); // index of start and end of a bracket
    private readonly HashSet<int> modifiedCells = new();
    public bool useNumbers = false;
    public int steps = 0;
    public long maxSteps = 1_000_000;
    public Action<UpdateType>? OnUpdate;
    public int cell = 0;
    private void PreProcess(string content)
    {
        Stack<int> stack = new Stack<int>();
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '[')
            {
                stack.Push(i);
            }
            else if (content[i] == ']')
            {
                int openIndex = stack.Pop();
                jumpTable[openIndex] = i;
                jumpTable[i] = openIndex;
            }
        }
    }
    public void Parse(string rawContent)
    {
        string rawFiltered = string.Join("\n",
            rawContent
                .Split('\n')
                .Select(line => line.Split('#')[0])
        );

        string content = new string(
            rawFiltered
                .Where(c => "+-<>[],.".Contains(c))
                .ToArray()
        );
        PreProcess(content);
        for (int i = 0; i < content.Length; i++)
        {
            steps++;
            if (maxSteps != -1 && steps > maxSteps) throw new BFExecutionException("Execution limit reached", steps);
            char c = content[i];
            switch (c)
            {
                case '+': memory[cell]++; modifiedCells.Add(cell); break;
                case '-': memory[cell]--; modifiedCells.Add(cell); break;
                case '>': cell = (cell + 1) % memory.Length; break;
                case '<': cell = (cell - 1 + memory.Length) % memory.Length; break;
                case '.':
                    var val = memory[cell];
                    if (useNumbers)
                    {
                        output.Append(memory[cell]);
                    }
                    else
                    {
                        output.Append((char)memory[cell]);
                    }
                    break;
                case ',':
                    OnUpdate?.Invoke(UpdateType.WaitingForInput);
                    var key = Console.ReadKey(intercept: true);
                    memory[cell] = (byte)key.KeyChar;
                    modifiedCells.Add(cell);
                    OnUpdate?.Invoke(UpdateType.Default);
                    break;
                case '[': if (memory[cell] == 0) i = jumpTable[i]; break;
                case ']': if (memory[cell] != 0) i = jumpTable[i]; break;
            }
        }
    }
    public string GetActiveMemory()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < memory.Length; i++)
        {
            if (memory[i] != 0)
                sb.AppendLine($"{i}: {memory[i]} ({(char)memory[i]})");
        }
        return sb.ToString();
    }
}
