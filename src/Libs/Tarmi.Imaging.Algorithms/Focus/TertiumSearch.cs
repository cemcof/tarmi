using Tarmi.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Imaging.Algorithms.Focus;

public class TertiumSearch : IMaximumSearch
{
    public static async Task<T> FindMaximumAsync<T, TUnit>(Func<T, CancellationToken, Task<double>> objectiveFunction, RangeDescriptorWithStep<T> range, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
        where T : IArithmeticQuantity<T, TUnit, double>
        where TUnit : Enum
    {
        var step = (range.Max - range.Min) / 4;
        var midX = (range.Max + range.Min) / 2;
        var midY = await objectiveFunction.Invoke(midX, cancellationToken);

        var numberOfSteps = (int)Math.Ceiling(Math.Log(step.As(range.Step.Unit) / range.Step.Value, 2));
        logger.LogDebug("Autofocus: {NumberOfSteps}", numberOfSteps);
        for (int stepIndex = 0; stepIndex < numberOfSteps; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(("Focus optimization", Ratio.FromDecimalFractions((double)stepIndex / numberOfSteps)));
            var lowX = midX - step;
            var highX = midX + step;
            var lowY = await objectiveFunction.Invoke(lowX, cancellationToken);
            var highY = await objectiveFunction.Invoke(highX, cancellationToken);
            logger.LogDebug("Autofocus: ({Low}, {Mid}, {High})", lowY, midY, highY);
            if (lowY > midY && lowY > highY)
            {
                logger.LogDebug("Autofocus: Selecting lower distance.");
                midX = lowX;
                midY = lowY;
            }
            else if (highY > midY)
            {
                logger.LogDebug("Autofocus: Selecting higher distance.");
                midX = highX;
                midY = highY;
            }
            else
            {
                logger.LogDebug("Autofocus: Narrowing.");
            }
            step /= 2;
        }
        progress.Report(("Focus optimization", Ratio.FromDecimalFractions(1)));
        return midX;
    }
}
