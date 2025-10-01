using Betrian.App.Infrastructure;
using Betrian.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.Imaging.Algorithms.Focus;

public class SectionSearch : IMaximumSearch
{
    private static readonly double GoldenRatioInverse = 2 / (1 + Math.Sqrt(5));

    public static async Task<T> FindMaximumAsync<T, TUnit>(Func<T, CancellationToken, Task<double>> objectiveFunction, RangeDescriptorWithStep<T> range, IProgress<(string, Ratio)> progress,ILogger logger, CancellationToken cancellationToken)
        where T : IArithmeticQuantity<T, TUnit, double>
        where TUnit : Enum
    {
        using var activity = AppTelemetry.ImageAlgoActivitySource.StartActivity(nameof(FindMaximumAsync));

        var difference = range.Max - range.Min;

        var numberOfSteps = (int)Math.Ceiling(Math.Log(range.Step.Value / difference.As(range.Step.Unit), GoldenRatioInverse));

        var offset = difference * GoldenRatioInverse;
        var highX = range.Min + offset;
        var lowX = range.Max - offset;

        progress.Report(("Focus optimization", Ratio.FromDecimalFractions(0.0 / (numberOfSteps + 2))));
        double lowY = await objectiveFunction(lowX, cancellationToken);
        progress.Report(("Focus optimization", Ratio.FromDecimalFractions(1.0 / (numberOfSteps + 2))));
        double highY = await objectiveFunction(highX, cancellationToken);

        difference = highX - lowX;

        for (int step = 0; step < numberOfSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(("Focus optimization", Ratio.FromDecimalFractions((double)(step + 2) / (numberOfSteps + 2))));
            difference *= GoldenRatioInverse;
            logger.LogDebug("Autofocus scores: ({Low}, {High})", lowY, highY);
            // Shift to higher distance
            if (lowY < highY)
            {
                logger.LogDebug("Selecting higher distance.");
                lowX = highX;
                lowY = highY;
                highX += difference;
                highY = await objectiveFunction(highX, cancellationToken);
            }
            // Shift to lower distance
            else
            {
                logger.LogDebug("Selecting lower distance.");
                highX = lowX;
                highY = lowY;
                lowX -= difference;
                lowY = await objectiveFunction(lowX, cancellationToken);
            }
        }
        progress.Report(("Focus optimization", Ratio.FromDecimalFractions(1)));
        // Return the midpoint
        return (highX + lowX) * 0.5;
    }
}
