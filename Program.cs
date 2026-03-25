using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml;
using Spectre.Console;
using Spectre.Console.Cli;

var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("bfg.VERSION");
using var reader = new StreamReader(stream!);
var version = reader.ReadToEnd().Trim();

if (args.Contains("-v") || args.Contains("--version"))
{
    Console.WriteLine($"bfg v{version}");
    return 0;
}

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<RunCommand>("run")
        .WithDescription("Run a file.");

    config.AddCommand<VisualizeCommand>("visualize")
        .WithDescription("Visualize a file.");
    config.AddCommand<StringCommand>("string");
});
app.SetDefaultCommand<RunCommand>();

return app.Run(args);




public sealed class RunCommand : Command<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The file to run")]
        public string File { get; set; } = "";

        [CommandOption("--show-memory")]
        [Description("Show the memory after execution")]
        [DefaultValue(false)]
        public bool ShowMemory { get; set; }

        [CommandOption("-m|--meta")]
        [Description("Show elapsed time and step count after execution.")]
        [DefaultValue(false)]
        public bool Meta { get; set; }

        [CommandOption("-n|--num")]
        [Description("If true, displays output as numbers instead of letters")]
        [DefaultValue(false)]
        public bool Numeric { get; set; }

        [CommandOption("-q|--quiet")]
        [Description("If true, does not show any messages other than the output.")]
        [DefaultValue(false)]
        public bool Quiet { get; set; }

        [CommandOption("--max-steps")]
        [Description("The max amount of steps the program can use. Use 0 for infinite")]
        [DefaultValue(-1L)]
        public long MaxSteps { get; set; }

        [CommandOption("--ignore-invalid-instructions")]
        [DefaultValue(false)]
        public bool IgnoreInvalidInstructions { get; set; }

        [CommandOption("--no-stream")]
        [Description("Wait for the full program to finish, then prints the output.")]
        [DefaultValue(false)]
        public bool NoStream { get; set; }

        [CommandOption("--delay")]
        [Description("Delay after outputting a character in miliseconds. Only works when streaming")]
        [DefaultValue(0)]
        public int Delay { get; set; }
    }
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(settings.File);
        if (!fileInfo.Exists)
        {
            AnsiConsole.MarkupLine($"[red]File \"{fileInfo.Name}\" could not be found.[/]");
            return 1;
        }

        var prog = new BFProgram();
        prog.maxSteps = settings.MaxSteps;
        prog.useNumbers = settings.Numeric;
        prog.ignoreInvalidInstructions = settings.IgnoreInvalidInstructions;

        bool error = false;
        string content = File.ReadAllText(fileInfo.FullName);
        var sw = new System.Diagnostics.Stopwatch();

        if (!settings.Quiet)
        {
            AnsiConsole.Status().Start("Executing...", ctx =>
            {
                prog.OnUpdate = updateType =>
                {
                    switch (updateType)
                    {
                        case UpdateType.Default:
                            ctx.Status("Executing...");
                            break;
                        case UpdateType.WaitingForInput:
                            ctx.Status(settings.Meta ? $"Waiting for input in cell {prog.cell}" : "Waiting for input");
                            ctx.Spinner(Spinner.Known.Arc);
                            break;
                    }
                };
                if (!settings.NoStream)
                {
                    string output = "";
                    ctx.Spinner(new EmptySpinner());
                    prog.OnOutput = s =>
                    {
                        output += s;
                        ctx.Status(output);
                        if (settings.Delay > 0)
                            Thread.Sleep(settings.Delay);
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
                if (!settings.NoStream)
                {
                    prog.OnOutput = s =>
                    {
                        AnsiConsole.Write(s);
                        if (settings.Delay > 0)
                            Thread.Sleep(settings.Delay);
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

            if (settings.ShowMemory && !settings.Quiet)
                AnsiConsole.WriteLine(prog.GetActiveMemory());
        }

        if (settings.Meta && !settings.Quiet)
            AnsiConsole.MarkupLine($"[grey]took {sw.Elapsed.TotalMilliseconds:N2}ms and {prog.steps} steps[/]");

        return error ? 1 : 0;
    }
}




public sealed class VisualizeCommand : Command<VisualizeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("The file to visualize")]
        public string File { get; set; } = "";
    }
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[yellow]Visualize is not yet implemented.[/]");
        return 0;
    }
}
public sealed class StringCommand : Command<StringCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<string>")]
        [Description("The string to convert to brainfuck")]
        public string[] String { get; set; } = Array.Empty<string>();

        [CommandOption("-o|--output")]
        [Description("The file that the output gets printed to")]
        [DefaultValue(null)]
        public FileInfo? Output { get; set; }


    }
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string bf = StringToBf.GenerateBf(string.Join(" ", settings.String));
        FileInfo? output = settings.Output;

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

        using (var stream = output.Open(FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(bf);
        }

        AnsiConsole.MarkupLine($"[green]Successfully written to {output.FullName}[/]");
        return 0;
    }
}