using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.Projects.Transactions;
using CFLMnavi.VirtualDevices;
using CFLMnavi.VirtualDevices.Implementation;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.CflmNavi.App.Services.Application;
public class ZStackGrabbingService
{
    private readonly ILuminescenceMode _luminescenceMode;
    private readonly ILogger _logger;

    public ZStackGrabbingService(ILuminescenceMode luminescenceMode, ILogger<ZStackGrabbingService> logger)
    {
        _luminescenceMode = luminescenceMode;
        _logger = logger;
    }

    public async Task GrabZStackAsync(
        ObservableProject project, IStageNavigation stageNavigation, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        ZStackSettings settings, IProgress<(string, Ratio)> progress, Guid? linkId, CancellationToken cancellationToken
    )
    {
        using var zStackTransaction = new ZStackCreationTransaction(project, stageCameraView, stageNavigation.GetPlanePosition, linkId);
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(zStackTransaction.Cancel);
        try
        {
            _luminescenceMode.StopGrabbing();
            await _luminescenceMode.GrabZStackAsync(settings, stageCameraView, pipelineGrabber, zStackTransaction.AddImage, zStackTransaction.AddMipImage, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Z-stack grabbing failed.");
        }
    }
}
