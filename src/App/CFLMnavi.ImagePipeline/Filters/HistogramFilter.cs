using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using CFLMnavi.Configuration;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace CFLMnavi.ImagePipeline.Filters;

public sealed class HistogramFilter : FilterBase
{
    // min max range is defined 0..255 to reflect defined number of bins 256
    // for 8-bit images it's directly mapped, on 16-bit images it's scaled
    public const int BinsCount = 256;
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

        var histogram = image.Image.GetNormalizedHistogram(BinsCount);

        if (AutoEqualize)
        {
            OpenCvSharp.Mat mat = new();
            (_lowerBound, _upperBound) = image.Image.GetMinMaxHistogramValues(_autoEqualizeMin, _autoEqualizeMax);
        }

        _logger.Swallow(() => _histogramSource.OnNext(histogram));
        image.Image.UpdateImageByMinAndMaxHistogramValuesUsingLutInplace(_lowerBound, _upperBound);
    }
}
