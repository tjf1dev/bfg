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

    public Action<UpdateType>? OnUpdate;
    public Action<string>? OnOutput;
    public int cell = 0;
    public int steps = 0;


    public bool useNumbers = false;
    public long maxSteps = -1;
    public bool ignoreInvalidInstructions = false;

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
        string content = string.Join("\n",
            rawContent.Split('\n').Select(line => line.Split('#')[0])
        );

        PreProcess(content);

        int line = 1;
        int column = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '\n')
            {
                line++;
                column = 0;
                continue;
            }

            column++;

            if (char.IsWhiteSpace(c)) continue;

            if ("+-<>[],.".Contains(c))
            {
                steps++;
                if (maxSteps != -1 && steps > maxSteps)
                    throw new ExecutionException("Execution limit reached", steps);

                switch (c)
                {
                    case '+': memory[cell]++; modifiedCells.Add(cell); break;
                    case '-': memory[cell]--; modifiedCells.Add(cell); break;
                    case '>': cell = (cell + 1) % memory.Length; break;
                    case '<': cell = (cell - 1 + memory.Length) % memory.Length; break;
                    case '.':
                        {
                            string value = useNumbers
                                ? memory[cell].ToString()
                                : ((char)memory[cell]).ToString();

                            output.Append(value);
                            OnOutput?.Invoke(value);
                            break;
                        }
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
            else
            {
                if (ignoreInvalidInstructions)
                {
                    continue;
                }
                else
                {
                    throw new InvalidInstruction(
                        $"Invalid instruction at {line}:{column}: {c}", c
                    );
                }
            }
        }
    }
    public string GetActiveMemory()
    {
        StringBuilder sb = new();
        for (int i = 0; i < memory.Length; i++)
        {
            if (memory[i] != 0)
                sb.AppendLine($"{i}: {memory[i]} ({(char)memory[i]})");
        }
        return sb.ToString();
    }
}
