using System.Collections;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Spectre.Console;

class BFG
{
    static int Main(string[] args)
    {
        RootCommand rootCommand = new("Simple Brainfuck parser.");
        Argument<FileInfo> file = new("file")
        {
            Description = "The file to run",
        };
        Option<bool> showMemoryOption = new("--show-memory")
        {
            Description = "Show the memory after execution",
            DefaultValueFactory = c => false
        };
        Option<bool> showMeta = new("--meta", "-m")
        {
            Description = "Show elapsed time and step count after execution.",
            DefaultValueFactory = c => false
        };
        Option<bool> numeric = new("--num", "-n")
        {
            Description = "If true, displays output as numbers instead of letters",
            DefaultValueFactory = c => false
        };
        Option<bool> quiet = new("--quiet", "-q")
        {
            Description = "If true, does not show any messages other than the output.",
            DefaultValueFactory = c => false
        };
        Option<long> maxStepsOption = new("--max-steps", "-s")
        {
            Description = "The max amount of steps the program can use. use 0 for infinite",
            DefaultValueFactory = c => -1L
        };


        rootCommand.Arguments.Add(file);
        rootCommand.Options.Add(showMemoryOption);
        rootCommand.Options.Add(showMeta);
        rootCommand.Options.Add(numeric);
        rootCommand.Options.Add(quiet);
        rootCommand.Options.Add(maxStepsOption);


        ParseResult parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count == 0 && parseResult.GetValue(file) is FileInfo parsedFile)
        {
            if (!parsedFile.Exists)
            {
                AnsiConsole.MarkupLine($"[red]File \"{parsedFile.Name}\" could not be found.[/]");
                return 1;
            }
            var prog = new BFProgram();
            var meta = parseResult.GetValue(showMeta) is true;
            prog.maxSteps = parseResult.GetValue(maxStepsOption);
            prog.useNumbers = parseResult.GetValue(numeric);
            bool error = false;
            var sw = new System.Diagnostics.Stopwatch();
            TimeSpan duration = TimeSpan.Zero;
            bool isQuiet = parseResult.GetValue(quiet);
            if (!isQuiet)
            {
                AnsiConsole.Status().Start("Executing...", ctx =>
                       {
                           string content = File.ReadAllText(parsedFile.FullName);
                           prog.OnUpdate = updateType =>
                           {
                               switch (updateType)
                               {
                                   case UpdateType.Default:
                                       ctx.Status("Executing...");
                                       break;
                                   case UpdateType.WaitingForInput:
                                       if (meta)
                                       {
                                           ctx.Status($"Waiting for input in cell {prog.cell}");
                                       }
                                       else
                                       {
                                           ctx.Status("Waiting for input");
                                       }
                                       ctx.Spinner(Spinner.Known.Arc);
                                       break;
                               }
                           };

                           try
                           {
                               sw.Start();

                               prog.Parse(content);
                               sw.Stop();
                           }
                           catch (BFExecutionException)
                           {
                               sw.Stop();
                               AnsiConsole.MarkupLine("[red]Execution limit reached.[/]");
                               error = true;
                           }
                           finally
                           {
                               duration = sw.Elapsed;
                           }
                       });
            }
            else
            {
                string content = File.ReadAllText(parsedFile.FullName);
                try
                {
                    sw.Start();
                    prog.Parse(content);
                    sw.Stop();
                }
                catch (BFExecutionException)
                {
                    sw.Stop();
                    error = true;
                }
                finally
                {
                    duration = sw.Elapsed;
                }
            }

            AnsiConsole.WriteLine(prog.GetOutput());

            if (error)
            {
                if (!isQuiet) AnsiConsole.MarkupLine("[red]Execution limit reached.[/]");
                if (meta && !isQuiet) AnsiConsole.MarkupLine($"[grey]took {duration.TotalMilliseconds:N2}ms and {prog.steps} steps[/]");
                return 1;
            }
            if (parseResult.GetValue(showMemoryOption) is true && !isQuiet) AnsiConsole.WriteLine(prog.GetActiveMemory());
            if (meta && !isQuiet) AnsiConsole.MarkupLine($"[grey]took {duration.TotalMilliseconds:N2}ms and {prog.steps} steps[/]");
            return 0;
        }
        foreach (ParseError parseError in parseResult.Errors)
        {
            AnsiConsole.MarkupLine($"[red]{parseError.Message}[/]");
        }
        return 1;
    }
}