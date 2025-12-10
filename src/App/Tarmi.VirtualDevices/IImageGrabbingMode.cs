using Tarmi.Imaging.Common;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface IImageGrabbingMode
{
    Length HorizontalFieldWidth { get; }
    Length VerticalFieldWidth { get; }
    bool IsGrabbingActive { get; }
    IObservable<bool> GrabbingActiveChanges { get; }
    IObservable<ImageWithMetadata> Image { get; }
    Task StartGrabbingAsync(CancellationToken cancellationToken);
    void StopGrabbing();
    Task FocusAsync(double change, CancellationToken cancellationToken);
    Task FocusAtAsync(Length focusLength, CancellationToken cancellationToken);
    Length GetCurrentFocusLength();

    [Obsolete("Use IImagingPipelineGrabber.")]
    Task<ImageWithMetadata> GrabImageAsync();
}
