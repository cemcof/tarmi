using Microsoft.Extensions.Logging;
﻿using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.Projects.Transactions;
using Tarmi.VirtualDevices;
using Tarmi.VirtualDevices.Implementation;
using UnitsNet;

namespace Tarmi.App.Services.Application;
public class ZStackGrabbingService
{
    private readonly IVirtualDevice _virtualDevice;
    private readonly IZStackGrabbingMode _grabbingMode;
    private readonly ILogger _logger;

    public ZStackGrabbingService(IVirtualDevice virtualDevice, IZStackGrabbingMode grabbingMode, ILogger<ZStackGrabbingService> logger)
    {
        _virtualDevice = virtualDevice;
        _grabbingMode = grabbingMode;
        _logger = logger;
    }

    public async Task GrabZStackAsync(
        ObservableProject project, IStageNavigation stageNavigation, StageCameraView stageCameraView, IImagingPipelineGrabber pipelineGrabber,
        ZStackOptions options, IProgress<(string, Ratio)> progress, Guid? linkId, CancellationToken cancellationToken
    )
    {
        using var zStackTransaction = new ZStackCreationTransaction(project, stageCameraView, stageNavigation.GetPlanePosition, linkId);
        using var tileSetLayerTransactionCancellation = cancellationToken.Register(zStackTransaction.Cancel);
        try
        {
            _virtualDevice.StopGrabbing();
            await _grabbingMode.GrabZStackAsync(options, stageCameraView, pipelineGrabber, zStackTransaction.AddImage, zStackTransaction.AddMipImage, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Z-stack grabbing failed.");
        }
    }
}
