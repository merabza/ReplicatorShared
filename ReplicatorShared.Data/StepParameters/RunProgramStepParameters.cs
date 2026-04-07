using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.StepParameters;

public sealed class RunProgramStepParameters
{
    public RunProgramStepParameters(string program, string arguments)
    {
        Program = program;
        Arguments = arguments;
    }

    public string Program { get; } //პროგრამა. რომელიც უნდა გაეშვას
    public string Arguments { get; } //პროგრამის არგუმენტები

    public static RunProgramStepParameters? Create(ILogger? logger, bool useConsole, string? program, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(program))
        {
            StShared.WriteErrorLine("program path does not specified", useConsole, logger);
            return null;
        }

        if (string.IsNullOrWhiteSpace(arguments))
        {
            StShared.WriteErrorLine("program arguments does not specified", useConsole, logger);
            return null;
        }

        return new RunProgramStepParameters(program, arguments);
    }
}
