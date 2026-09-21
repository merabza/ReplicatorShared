using System;
using SystemTools.SystemToolsShared;

namespace ReplicatorShared.Data.Models;

public sealed class JobSchedule : ItemData
{
    //ეს კლასი განკუთვნილია პროცესების გაშვების ინიცირებისათვის. ანუ როდის უნდა გაეშვას პროცესები.
    //ამ შედულეს საშუალებით ხდება ყველა არსებული პროცესის გაშვების მცდელობა.
    //პროცესების თანმიმდვერობა განისაზღვრება რიგითი ნომრების მიხედვით
    //პროცესები შეიძლება გაეშვას პარალელურად, ამისათვის გამოიყენება პროცესების ხაზი.
    //მიუხედავად ამ შედულეს საშუალებით გაშვებისა, შეიძლება თითოეული ნაბიჯის გაშვება გაკონტროლდეს ამ ნაბიჯზე მითითებული პარამეტრებით.

    public bool Enabled { get; set; } // მიუთითებს დასაშვებია თუ არა ამ შედულეს გამოყენება.

    public EScheduleType ScheduleType
    {
        get;
        set;
    } //შედულეს ტიპი შეიძლება იყოს: პროგრამის გაშვებისას (AtStart), ერთხელ მითითებულ დროს (Once), ყოველდღიურად (Daily)
    //მომავალში შეიძლება დაემატოს: ყოველკვირეული (Weekly), ყოველთვიური (Monthly), როცა პროცესორი დაკავებული არ არის (WhenCpuIdle)

    public DateTime RunOnceDateTime
    {
        get;
        set;
    } //Once თუ შედულეს ტიპი არჩეულია Once, მაშინ ეს თარიღი და დრო მიუთითებს როდის უნდა გაეშვას იმ ერთადერთხელ პროცესი.

    public int FreqInterval
    {
        get;
        set;
    } //Daily თუ შედულეს ტიპი არჩეულია Daily, მაშინ ეს მთელი რიცხვი მუთითებს რამდენ დღეში ერთხელ უნდა გაეშვას პროცესი.
    //თუ მომავალში გამოყენებული იქნება Weekly, ან Monthly შედულეს ტიპი, მაშინ ეს რიცხვი შესაბამისად აღნიშნავს რამდენ კვირაში ერთხელ და რამდენ თვეში ერთხელ უნდა ჩაირთოს პროცესი

    //თუ არჩეული არ არის ერთჯერადი შესრულება, შესაძლებელია განვსაზღვროთ რა პერიოდის განმავლობაში იმუშავებს ეს შედულე.
    //შესაძლებელია დაფიქსირდეს მხოლოდ საწყისი თარიღი. ან ორივე საწყისი და საბოლოო თარიღები
    public DateTime DurationStartDate { get; set; } //AtStart //Daily
    public DateTime DurationEndDate { get; set; } //AtStart //Daily

    //public DateTime ActiveStartDate { get; set; } //
    //public DateTime ActiveEndDate { get; set; } //Daily

    public EDailyFrequency DailyFrequencyType
    {
        get;
        set;
    } //Daily ეს ველი განსაზღვრავს დღის განმავლობაში ერთხელ ეშვება პროცესი, თუ რამდენჯერმე რაღაც პერიოდულობით.

    //Daily თუ მითითებულია ყოველდღიური შედულე, მაშინ აქ მითითებული დრო გამოიყენება იმის მისათითებლად დღის რომელ დროს უნდა გაეშვას პროცესი
    //თუ არჩეულია დღეში ერთი გაშვება, მაშინ ეს დრო მიუთითებს როდის უნდა გაეშვას ეს პროცესი.
    //თუ არჩეულია დღეში რამდენიმე გაშვება, მაშინ ეს დრო მიუთითებს რომელი დროიდან იწყება პირველი პროცესი.
    public TimeSpan ActiveStartDayTime { get; set; }

    public TimeSpan ActiveEndDayTime
    {
        get;
        set;
    } //Daily თუ მითითებულია ყოველდღიური შედულე და არჩეულია დღეში რამდენიმე გაშვება. მაშინ აქ ეთითება დღის რომელ დრომდე უნდა გაეშვას პროცესი

    //თუ არჩეულია დღეში რამდენიმე გაშვება,
    public EEveryMeasure FreqSubDayType
    {
        get;
        set;
    } //Daily მიუთითებს საათები გამოიყენება ყოველი გაშვების ინტერვალის დასადგენად, თუ წუთები

    public int FreqSubDayInterval { get; set; } //Daily ყოველი რამდენი საათის თუ წუთების შემდეგ უნდა მოხდეს გაშვება

    //public bool StopAtEndDayTime { get; set; } //Daily

    //public DateTime NextRunDate { get; set; }
    //public int FreqRelativeInterval { get; set; }
    //public int FreqRecurrenceFactor { get; set; }

    //public List<string> JobStepNames { get; set; } //ნაბიჯების სახელები, რომლებიც უნდა შესრულდეს ამ შედულეს ფარგლებში.
}
