# bfg
a silly brainfuck interpreter i made in c#

> [!WARNING]
> as always, this may not work at all, please report any issues ;)
# download
### Arch Linux / Arch-based distros
available as an [AUR package](https://aur.archlinux.org/packages/bfg-bin)
#### install using yay
```sh
yay -S bfg-bin
```
### other
download an executable from the [Releases](https://github.com/tjf1dev/bfg/releases/latest) page.  
on windows, you will have to run it in the terminal - just running the .exe won't work

# how it works
it works just like any other bf interpreter!  
![](images/run.png)
## comments
bfg supports comments prefixed with `#`
#### comments: do's and dont's
```
✅: # this code: ++++++++[>++++++++<-]>.+. wont be exeucted.
❌: hi! this is a comment, but without the prefix. (, and . get parsed)

✅: +++ # this is a correct inline commment    
❌: +++ this is an inline comment (would work, but this is not recommended)
❌: +++ this is another inline comment. (breaks things - . gets parsed)
```

## syntax
```
Usage:
  bfg <file> [command] [options]

Arguments:
  <file>  The file to run

Options:
  --show-memory                  Show the memory after execution
  -m, --meta                     Show elapsed time and step count after execution.
  -n, --num                      If true, displays output as numbers instead of letters
  -q, --quiet                    If true, does not show any messages other than the output.
  --max-steps <max-steps>        The max amount of steps the program can use. Use 0 for infinite [default: -1]
  --ignore-invalid-instructions
  --no-stream                    Wait for the full program to finish, then prints the output.
  --delay <delay>                Delay after outputting a character in miliseconds. Only works when streaming [default: 0]
  -?, -h, --help                 Show help and usage information
  --version                      Show version information

Commands:
  run <file>        Run a file.
  visualize <file>  Visualize a file.
  string <string>   
```

# build
install the .NET SDK first
```
git clone https://github.com/tjf1dev/bfg
cd bfg
dotnet run examples/helloworld.bf -- -m
```
