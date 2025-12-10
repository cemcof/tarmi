using System.Runtime.Versioning;
using System.Reactive.Linq;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.TileSet.ImageSimulator.Abstractions;
using Tarmi.VirtualDevices;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;

[assembly: SupportedOSPlatform("windows")]

namespace Tarmi.TileSet.ImageSimulator;

internal class TileSetSimulator : ITileSetImageSimulator
{
    private readonly IStageNavigation _stageNavigation;
    private volatile StageCameraView _currentContextCameraView = StageCameraView.Unknown;
    private volatile StagePosition _currentContextStagePosition = StagePosition.Zero;
    private volatile LengthPoint _currentContextPlanePosition = LengthPoint.Zero;
    private TileSetDefinition? _tileSetDefinition = null;

    public TileSetSimulator(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IInstrument instrument, IProjectManager projectManager)
    {
        _stageNavigation = stageNavigation;
        _ = Observable.CombineLatest(
            instrument.Stage,
            safeStageControlling.ActiveCameraViewChanges,
            projectManager.ActiveProject,
            (stage, cameraView, activeProject) => (stage, cameraView, activeProject)
        ).Subscribe(state => HandleStageAndViewChanges(state.stage, state.cameraView, state.activeProject));

    }

    private void HandleStageAndViewChanges(StageState stage, StageCameraView cameraView, ObservableProject? project)
    {
        if (!stage.IsMoving && !stage.CurrentPosition.Equals(_currentContextStagePosition))
        {
            _currentContextStagePosition = stage.CurrentPosition;
            if (_currentContextCameraView != cameraView && project is not null)
            {
                _tileSetDefinition?.Dispose();
                _tileSetDefinition = cameraView switch
                {
                    StageCameraView.FIB_RightAngle => FibRightAngleTileSetDefinition.Create(project.Holder),
                    StageCameraView.SEM => SemTileSetDefinition.Create(project.Holder),
                    _ => null,
                };
                _currentContextCameraView = cameraView;
            }

            _currentContextPlanePosition = _currentContextCameraView != StageCameraView.Unknown ?
                _stageNavigation.GetPlanePosition(_currentContextStagePosition, cameraView) :
                LengthPoint.Zero;
        }
    }

    public StageCameraView CurrentContextCameraView => _currentContextCameraView;

    public bool IsViewSupported(StageCameraView cameraView)
    {
        return cameraView switch
        {
            StageCameraView.FIB_RightAngle => true,
            StageCameraView.SEM => true,
            _ => false,
        };
    }

    public ImageWithMetadata GrabOne()
    {
        if (IsViewSupported(_currentContextCameraView) && _tileSetDefinition is not null)
        {
            return _tileSetDefinition.GetImage(_currentContextPlanePosition);
        }

        throw new NotSupportedException();
    }
}
