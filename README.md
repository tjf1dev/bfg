# bfg
a silly brainfuck interpreter i made in c#

> [!WARNING]
> as always, this may not work at all, please report any issues ;)
 
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
USAGE:
    bfg <file> [OPTIONS] [COMMAND]

ARGUMENTS:
    <file>    The file to run

OPTIONS:
                                         DEFAULT                                                                                
    -h, --help                                      Prints help information                                                     
        --show-memory                               Show the memory after execution                                             
    -m, --meta                                      Show elapsed time and step count after execution                            
    -n, --num                                       If true, displays output as numbers instead of letters                      
    -q, --quiet                                     If true, does not show any messages other than the output                   
        --max-steps                      -1         The max amount of steps the program can use. Use 0 for infinite             
        --ignore-invalid-instructions                                                                                           
        --no-stream                                 Wait for the full program to finish, then prints the output                 
        --delay                          0          Delay after outputting a character in miliseconds. Only works when streaming

COMMANDS:
    run <file>          Run a file      
    visualize <file>    Visualize a file
    string <string>       
```
# download
download an executable from the [Releases](/tjf1dev/bfg/releases) page.  
on windows, you will have to run it in the terminal - just running the .exe won't work

# build
install the .NET SDK first
```
git clone https://github.com/tjf1dev/bfg
cd bfg
dotnet run examples/helloworld.bf -- -m
```
