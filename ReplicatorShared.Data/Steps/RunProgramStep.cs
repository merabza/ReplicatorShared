using System.Net.Http;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using ReplicatorShared.Data.StepParameters;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

public sealed class RunProgramStep : JobStep
{
    public string? Program { get; set; } //პროგრამა. რომელიც უნდა გაეშვას
    public string? Arguments { get; set; } //პროგრამის არგუმენტები

    public override ProcessesToolAction? GetToolAction(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, ProcessManager processManager,
        ReplicatorParameters parameters, string procLogFilesFolder)
    {
        var par = RunProgramStepParameters.Create(logger, useConsole, Program, Arguments);

        if (par is not null)
        {
            return new RunProgramStepCommand(logger, useConsole, ProcLineId, par);
        }

        StShared.WriteErrorLine("parameters does not created, RunProgramStep did not run", useConsole, logger);
        return null;
    }
}
