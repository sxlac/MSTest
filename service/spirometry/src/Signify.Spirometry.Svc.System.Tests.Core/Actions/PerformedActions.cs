using Signify.Spirometry.Svc.System.Tests.Core.Models.Database;
using  Signify.Spirometry.Svc.System.Tests.Core.Constants;
using ResultsReceived = Signify.Spirometry.Core.Events.Akka.ResultsReceived;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signify.Spirometry.Svc.System.Tests.Core.Actions;

public class PerformedActions : BaseTestActions
{

    protected static void ValidateResults(Dictionary<int, string> answersDict, SpiroResults spiro, ResultsReceived results)
    {
        foreach (var key in answersDict.Keys)
        {
            switch (key)
            {
                case Answers.NeverCoughAnswerId:
                case Answers.RarelyCoughAnswerId:
                case Answers.SometimesCoughAnswerId:
                case Answers.OftenCoughAnswerId:    
                case Answers.VeryOftenCoughAnswerId:
                    Assert.AreEqual(answersDict[key], spiro.CoughMucusOccurrenceFrequencyValue);
                    Assert.AreEqual(answersDict[key], results.Results.CoughMucusOccurrenceFrequency);
                    break;
                case Answers.NeverWheezyChestAnswerId:
                case Answers.RarelyWheezyChestAnswerId:
                case Answers.SometimesWheezyChestAnswerId:
                case Answers.OftenWheezyChestAnswerId:
                case Answers.VeryOftenWheezyChestAnswerId:
                    Assert.AreEqual(answersDict[key], spiro.NoisyChestOccurrenceFrequencyValue);
                    Assert.AreEqual(answersDict[key], results.Results.NoisyChestOccurrenceFrequency);
                    break;
                case Answers.NeverBreathShortnessAnswerId:
                case Answers.RarelyBreathShortnessAnswerId:
                case Answers.SometimesBreathShortnessAnswerId:
                case Answers.OftenBreathShortnessAnswerId:
                case Answers.VeryOftenBreathShortnessAnswerId:
                    Assert.AreEqual(answersDict[key], spiro.ShortnessOfBreathPAOccurrenceValue);
                    Assert.AreEqual(answersDict[key], results.Results.ShortnessOfBreathPhysicalActivityOccurrenceFrequency);
                    break;
            }

            switch (key)
            {
                case Answers.HadWheezingNoAnswerId:
                    Assert.AreEqual("N", results.Results.HadWheezingPast12mo);
                    Assert.AreEqual("No", spiro.HadWheezingPast12moTrileanType);
                    break;
                case Answers.HadWheezingYesAnswerId:
                    Assert.AreEqual("Y", results.Results.HadWheezingPast12mo);
                    Assert.AreEqual("Yes", spiro.HadWheezingPast12moTrileanType);
                    break;
                case Answers.HadWheezingUnknownAnswerId:
                    Assert.AreEqual("U", results.Results.HadWheezingPast12mo);
                    Assert.AreEqual("Unknown", spiro.HadWheezingPast12moTrileanType);
                    break;
                case Answers.ShortBreathatRestNoAnswerId:
                    Assert.AreEqual("N", results.Results.GetsShortnessOfBreathAtRest);
                    Assert.AreEqual("No", spiro.GetsShortnessOfBreathAtRestTrileanType);
                    break;
                case Answers.ShortBreathatRestYesAnswerId:
                    Assert.AreEqual("Y", results.Results.GetsShortnessOfBreathAtRest);
                    Assert.AreEqual("Yes", spiro.GetsShortnessOfBreathAtRestTrileanType);
                    break;
                case Answers.ShortBreathatRestUnknownAnswerId:
                    Assert.AreEqual("U", results.Results.GetsShortnessOfBreathAtRest);
                    Assert.AreEqual("Unknown", spiro.GetsShortnessOfBreathAtRestTrileanType);
                    break;
                case Answers.ShortBreathExertionNoAnswerId:
                    Assert.AreEqual("N", results.Results.GetsShortnessOfBreathWithMildExertion);
                    Assert.AreEqual("No", spiro.GetsShortnessOfBreathWithMildExertionTrileanType);
                    break;
                case Answers.ShortBreathExertionYesAnswerId:
                    Assert.AreEqual("Y", results.Results.GetsShortnessOfBreathWithMildExertion);
                    Assert.AreEqual("Yes", spiro.GetsShortnessOfBreathWithMildExertionTrileanType);
                    break;
                case Answers.ShortBreathExertionUnknownAnswerId:
                    Assert.AreEqual("U", results.Results.GetsShortnessOfBreathWithMildExertion);
                    Assert.AreEqual("Unknown", spiro.GetsShortnessOfBreathWithMildExertionTrileanType);
                    break;
            }
        }
    }
    
}