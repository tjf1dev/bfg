using System.CommandLine;
using System.Reflection;
using Spectre.Console;

public class Bfg
{
    public static async Task<int> Main(string[] args)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("bfg.VERSION");
        using var reader = new StreamReader(stream!);
        var version = reader.ReadToEnd().Trim();

        if (args.Contains("-v") || args.Contains("--version"))
        {
            Console.WriteLine($"bfg v{version}");
            return 0;
        }

        var fileArg = new Argument<FileInfo>("file") { Description = "The file to run" };
        var showMemoryOpt = new Option<bool>("--show-memory") { Description = "Show the memory after execution" };
        var metaOpt = new Option<bool>("--meta") { Description = "Show elapsed time and step count after execution." };
        metaOpt.Aliases.Add("-m");
        var numericOpt = new Option<bool>("--num") { Description = "If true, displays output as numbers instead of letters" };
        numericOpt.Aliases.Add("-n");
        var quietOpt = new Option<bool>("--quiet") { Description = "If true, does not show any messages other than the output." };
        quietOpt.Aliases.Add("-q");
        var maxStepsOpt = new Option<long>("--max-steps") { Description = "The max amount of steps the program can use. Use 0 for infinite", DefaultValueFactory = _ => -1L };
        var ignoreInvalidOpt = new Option<bool>("--ignore-invalid-instructions");
        var noStreamOpt = new Option<bool>("--no-stream") { Description = "Wait for the full program to finish, then prints the output.", DefaultValueFactory = _ => false };
        var delayOpt = new Option<int>("--delay") { Description = "Delay after outputting a character in miliseconds. Only works when streaming", DefaultValueFactory = _ => 0 };

        var runCommand = new Command("run", "Run a file.");
        runCommand.Arguments.Add(fileArg);
        runCommand.Options.Add(showMemoryOpt);
        runCommand.Options.Add(metaOpt);
        runCommand.Options.Add(numericOpt);
        runCommand.Options.Add(quietOpt);
        runCommand.Options.Add(maxStepsOpt);
        runCommand.Options.Add(ignoreInvalidOpt);
        runCommand.Options.Add(noStreamOpt);
        runCommand.Options.Add(delayOpt);
        runCommand.SetAction(result => RunFile(
            result.GetValue(fileArg)!,
            result.GetValue(showMemoryOpt),
            result.GetValue(metaOpt),
            result.GetValue(numericOpt),
            result.GetValue(quietOpt),
            result.GetValue(maxStepsOpt),
            result.GetValue(ignoreInvalidOpt),
            result.GetValue(noStreamOpt),
            result.GetValue(delayOpt)
        ));

        var vizFileArg = new Argument<FileInfo>("file") { Description = "The file to visualize" };
        var visualizeCommand = new Command("visualize", "Visualize a file.");
        visualizeCommand.Arguments.Add(vizFileArg);
        visualizeCommand.SetAction(_ =>
        {
            AnsiConsole.MarkupLine("[yellow]Visualize is not yet implemented.[/]");
        });

        var stringArg = new Argument<string[]>("string") { Description = "The string to convert to brainfuck", Arity = ArgumentArity.OneOrMore };
        var outputOpt = new Option<FileInfo?>("--output") { Description = "The file that the output gets printed to" };
        outputOpt.Aliases.Add("-o");
        var stringCommand = new Command("string");
        stringCommand.Arguments.Add(stringArg);
        stringCommand.Options.Add(outputOpt);
        stringCommand.SetAction(result => RunString(
            result.GetValue(stringArg)!,
            result.GetValue(outputOpt)
        ));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(runCommand);
        rootCommand.Subcommands.Add(visualizeCommand);
        rootCommand.Subcommands.Add(stringCommand);
        rootCommand.Arguments.Add(fileArg);
        rootCommand.Options.Add(showMemoryOpt);
        rootCommand.Options.Add(metaOpt);
        rootCommand.Options.Add(numericOpt);
        rootCommand.Options.Add(quietOpt);
        rootCommand.Options.Add(maxStepsOpt);
        rootCommand.Options.Add(ignoreInvalidOpt);
        rootCommand.Options.Add(noStreamOpt);
        rootCommand.Options.Add(delayOpt);
        rootCommand.SetAction(result => RunFile(
            result.GetValue(fileArg)!,
            result.GetValue(showMemoryOpt),
            result.GetValue(metaOpt),
            result.GetValue(numericOpt),
            result.GetValue(quietOpt),
            result.GetValue(maxStepsOpt),
            result.GetValue(ignoreInvalidOpt),
            result.GetValue(noStreamOpt),
            result.GetValue(delayOpt)
        ));

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static int RunFile(FileInfo fileInfo, bool showMemory, bool meta, bool numeric, bool quiet, long maxSteps, bool ignoreInvalid, bool noStream, int delay)
    {
        if (!fileInfo.Exists)
        {
            AnsiConsole.MarkupLine($"[red]File \"{fileInfo.Name}\" could not be found.[/]");
            return 1;
        }

        var prog = new BFProgram();
        prog.maxSteps = maxSteps;
        prog.useNumbers = numeric;
        prog.ignoreInvalidInstructions = ignoreInvalid;

        bool error = false;
        string content = File.ReadAllText(fileInfo.FullName);
        var sw = new System.Diagnostics.Stopwatch();

        if (!quiet)
        {
            AnsiConsole.Status().Start("Executing...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Default);
                prog.OnUpdate = updateType =>
                {
                    switch (updateType)
                    {
                        case UpdateType.Default:
                            ctx.Status("Executing...");
                            break;
                        case UpdateType.WaitingForInput:
                            ctx.Status(meta ? $"Waiting for input in cell {prog.cell}" : "Waiting for input");
                            ctx.Spinner(Spinner.Known.Arc);
                            break;
                    }
                };

                if (!noStream)
                {
                    string output = "";
                    prog.OnOutput = s =>
                    {
                        ctx.Spinner(new EmptySpinner());
                        output += s;
                        ctx.Status(Markup.Escape(output));
                        if (delay > 0)
                            Thread.Sleep(delay);
                    };
                }

                try
                {
                    sw.Start();
                    prog.Parse(content);
                    sw.Stop();
                }
                catch (ExecutionException ex)
                {
                    sw.Stop();
                    error = true;
                    AnsiConsole.MarkupLine($"[red]Execution limit reached after {ex.StepsTaken} steps[/]");
                }
                catch (InvalidInstruction ex)
                {
                    sw.Stop();
                    error = true;
                    AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                }
            });
        }
        else
        {
            try
            {
                sw.Start();
                if (!noStream)
                {
                    prog.OnOutput = s =>
                    {
                        AnsiConsole.Write(s);
                        if (delay > 0)
                            Thread.Sleep(delay);
                    };
                }
                prog.Parse(content);
                sw.Stop();
            }
            catch (ExecutionException)
            {
                sw.Stop();
                error = true;
            }
            catch (InvalidInstruction)
            {
                sw.Stop();
                error = true;
            }
        }

        if (!error)
        {
            AnsiConsole.WriteLine(prog.GetOutput());
            if (showMemory && !quiet)
                AnsiConsole.WriteLine(prog.GetActiveMemory());
        }

        if (meta && !quiet)
            AnsiConsole.MarkupLine($"[grey]took {sw.Elapsed.TotalMilliseconds:N2}ms and {prog.steps} steps[/]");

        return error ? 1 : 0;
    }

    private static int RunString(string[] strings, FileInfo? output)
    {
        string bf = StringToBf.GenerateBf(string.Join(" ", strings));

        if (output is null)
        {
            AnsiConsole.WriteLine(bf);
            return 0;
        }

        if (output.Exists)
        {
            if (!AnsiConsole.Confirm($"File \"{output.Name}\" already exists. Continue?"))
            {
                AnsiConsole.MarkupLine("[red]Aborting...[/]");
                return 1;
            }
        }

        using (var fs = output.Open(FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(fs))
        {
            writer.Write(bf);
        }

        AnsiConsole.MarkupLine($"[green]Successfully written to {output.FullName}[/]");
        return 0;
    }
}