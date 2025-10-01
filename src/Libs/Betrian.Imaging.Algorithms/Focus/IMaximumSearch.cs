using Betrian.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.Imaging.Algorithms.Focus
{
    public interface IMaximumSearch
    {
        static abstract Task<T> FindMaximumAsync<T, TUnit>(Func<T, CancellationToken, Task<double>> objectiveFunction, RangeDescriptorWithStep<T> range, IProgress<(string, Ratio)> progress, ILogger logger, CancellationToken cancellationToken)
            where T : IArithmeticQuantity<T, TUnit, double>
            where TUnit : Enum;
    }
}
