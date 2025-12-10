using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Configuration;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.ImagePipeline.Filters;

public sealed class HistogramFilter : FilterBase
{
    private readonly Ratio _autoEqualizeMin;
    private readonly Ratio _autoEqualizeMax;

    public HistogramFilter(ILogger logger, ApplicationConfig applicationConfig)
        : base(logger)
    {
        _autoEqualizeMin = applicationConfig.UserPreferences.Algorithms.AutoEqualize.Min;
        _autoEqualizeMax = applicationConfig.UserPreferences.Algorithms.AutoEqualize.Max;
    }

    private readonly Subject<SortedDictionary<int, double>> _histogramSource = new();

    public IObservable<SortedDictionary<int, double>> Histogram => _histogramSource.AsObservable();

    private int _lowerBound = 0;
    public int LowerBound
    {
        get => _lowerBound;
        set
        {
            Guard.IsBetweenOrEqualTo(value, 0, 255, nameof(LowerBound));
            Guard.IsLessThan(value, _upperBound, nameof(LowerBound));
            _lowerBound = value;
        }
    }

    private int _upperBound = 255;
    public int UpperBound
    {
        get => _upperBound;
        set
        {
            Guard.IsBetweenOrEqualTo(value, 0, 255, nameof(UpperBound));
            Guard.IsGreaterThan(value, _lowerBound, nameof(UpperBound));
            _upperBound = value;
        }
    }

    public bool AutoEqualize { get; set; }

    protected override void ProcessImageImplementation(ImageWithMetadata image)
    {
        if (!image.MemoryOrigin)
        {
            _logger.LogDebug("The image is loaded from disk, skipping histogram filter");
            return;
        }

        var histogram = image.Image.GetNormalizedHistogram();

        if (AutoEqualize)
        {
            (LowerBound, UpperBound) = image.Image.GetMinMaxHistogramValues(_autoEqualizeMin, _autoEqualizeMax);
        }

        _logger.Swallow(() => _histogramSource.OnNext(histogram));
        image.Image.UpdateImageByMinAndMaxHistogramValuesUsingLutInplace(LowerBound, UpperBound);
    }
}
