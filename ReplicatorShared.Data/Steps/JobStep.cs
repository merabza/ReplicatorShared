using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using Polly;
using Polly.Retry;
using ReplicatorShared.Data.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

//ერთი ამოცანა ამოცანების რიგში
public /*open*/ class JobStep : ItemData
{
    public bool Enabled { get; set; } //მიუთითებს დასაშვებია თუ არა ამ ნაბიჯის გამოყენება.

    public string? RetryStrategyName { get; set; } //რომელი Retry Strategy-ით უნდა გადეცეს ცდა ჩაშლის შემთხვევაში

    //public int SequentialNumber { get; set; } //ამოცანის რიგითი ნომერი. ამოცანების შესრულებისას ეს ნომერი გამოყენებული იქნება თანმიმდევრობის დასადგენად.
    public int
        ProcLineId
    {
        get;
        set;
    } //პროცესის ნომერი. თუ რამდენიმე პროცესის გაშვება შესაძლებელია პარალელურად, მაშინ მათი ეს ნომრები განსხვავებული უნდა იყოს. ერთნაირი ნომრებიანი შესრულდება თანმიმდევრობით

    //public EActionType ActionType { get; set; } //შესასრულებელი სამუშაოს ტიპი
    public int DelayMinutesBeforeStep { get; set; } //პროცესის გაშვებამდე მოვიცადოთ ამდენი წუთით
    public int DelayMinutesAfterStep { get; set; } //პროცესის დასრულების მერე მოვიცადოთ ამდენი წუთით

    //თუ შედულეს გამო ეს პროცესი გაშვების კანდიდატია, აქ დამატებითი ფილტრი გვაქვს დასაშვები პერიოდულობის მისაღწევად
    public TimeSpan HoleStartTime { get; set; } //დღეღამის განმავლობაში ამ დრომდე ამ პროცესის გაშვება არ შეიძლება

    public TimeSpan HoleEndTime { get; set; } //დღეღამის განმავლობაში ამ დროის მერე პროცესი არც უნდა გაეშვას.
    //თუ HoleStartTime > HoleEndTime, მაშინ HoleEndTime დროდან HoleStartTime დრომდე პროცესის გაშვება არ შეიძლება

    public EPeriodType PeriodType { get; set; } //პერიოდის ტიპი (მინიმუმ რა პერიოდში ერთხელ უნდა შესრულდეს ეს პროცესი)

    public int FreqInterval { get; set; } //სიხშირე. ანუ პერიოდების რაოდენობა, რომელში ერთხელაც უნდა შესრულდეს პროცესი.

    public DateTime StartAt { get; set; } //საყრდენი დროის წერტილი, საიდანაც აითვლება პერიოდები.

    public virtual ProcessesToolAction? GetToolAction(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, bool useConsole, ProcessManager processManager,
        ReplicatorParameters parameters, string procLogFilesFolder)
    {
        return null;
    }

    //თუ Step-ს მითითებული აქვს RetryStrategyName და ეს სახელი მოიძებნება პარამეტრებში,
    //აშენდეს Polly-ის ResiliencePipeline. წინააღმდეგ შემთხვევაში null - რეტრაი არ მოხდება.
    protected static ResiliencePipeline<bool>? BuildRetryPipeline(string? retryStrategyName,
        ReplicatorParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(retryStrategyName))
        {
            return null;
        }

        if (!parameters.RetryStrategyParameters.TryGetValue(retryStrategyName, out RetryStrategyParameters? rsp))
        {
            return null;
        }

        return new ResiliencePipelineBuilder<bool>().AddRetry(new RetryStrategyOptions<bool>
        {
            ShouldHandle =
                new PredicateBuilder<bool>().HandleResult(r => !r)
                    .Handle<Exception>(e => e is not OperationCanceledException),
            MaxRetryAttempts = rsp.MaxRetryAttempts,
            BackoffType = rsp.BackoffType,
            UseJitter = rsp.UseJitter,
            Delay = rsp.Delay,
            MaxDelay = rsp.MaxDelay
        }).Build();
    }
}
