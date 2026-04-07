using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ReplicatorShared.Data.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Steps;

//ერთი ამოცანა ამოცანების რიგში
public /*open*/ class JobStep : ItemData
{
    public bool Enabled { get; set; } //მიუთითებს დასაშვებია თუ არა ამ ნაბიჯის გამოყენება.

    //public int SequentialNumber { get; set; } //ამოცანის რიგითი ნომერი. ამოცანების შესრულებისას ეს ნომერი გამოყენებული იქნება თანმიმდევრობის დასადგენად.
    public int ProcLineId
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

    //public virtual bool Run(ILogger logger, ApAgentParameters parameters, Processes processes, CancellationToken cancellationToken = default)
    //{
    //  Console.WriteLine("Not Implemented Task Started");
    //  Thread.Sleep(1000);
    //  Console.WriteLine("Not Implemented Task Finished");

    //  return false;
    //}

    public virtual ProcessesToolAction? GetToolAction(ILogger logger, IHttpClientFactory httpClientFactory,
        bool useConsole, ProcessManager processManager, ApAgentParameters parameters, string procLogFilesFolder)
    {
        return null;
    }
}
