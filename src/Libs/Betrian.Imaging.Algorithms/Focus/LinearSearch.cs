using Betrian.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.Imaging.Algorithms.Focus;

public class LinearSearch : IMaximumSearch
{
    public static async Task<T> FindMaximumAsync<T, TUnit>(Func<T, CancellationToken, Task<double>> objectiveFunction, RangeDescriptorWithStep<T> range, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
        where T : IArithmeticQuantity<T, TUnit, double>
        where TUnit : Enum
    {
        var difference = range.Max - range.Min;
        var numberOfSteps = (int)(difference.As(range.Step.Unit) / range.Step.Value) + 1;
        double[] scores = new double[numberOfSteps];
        var position = range.Min;
        for (int stepIndex = 0; stepIndex < numberOfSteps; stepIndex++)
        {
            progress.Report(("Focus optimization", Ratio.FromDecimalFractions((double)stepIndex / numberOfSteps)));

            var score = await objectiveFunction.Invoke(position, cancellationToken);

            scores[stepIndex] = score;
            position += range.Step;
        }

        logger.LogDebug("Calculated autofocus scores");
        foreach (var (score, i) in scores.Select((score, index) => (score, index)))
        {
            logger.LogDebug("{Index}: {Score}", i, score);
        }

        progress.Report(("Focus optimization", Ratio.FromPercent(100)));

        var stableComparisons = 4;
        var (lowerBoundIndex, upperBoundIndex) = WPeakFinder.FindPeakBounds(scores, stableComparisons) ?? (0, scores.Length - 1);
        logger.LogDebug("({LowerBoundIndex}, {UpperBoundIndex})", lowerBoundIndex, upperBoundIndex);
        var index = Enumerable
            .Range(lowerBoundIndex, upperBoundIndex - lowerBoundIndex + 1)
            .MaxBy(i => scores[i]);
        logger.LogDebug("Autofocus selected {Index}", index);
        return Enumerable.Range(0, index).Aggregate(range.Min, (sum, _) => sum + range.Step);
    }
}
