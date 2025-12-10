using Tarmi.Models;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface IInstrumentStageControllingMode
{
    Task<StagePosition> GetStagePositionAsync();
    Task<bool> MoveStageAsync(StagePosition position);
    Task<bool> TiltStageAsync(Angle angle);
    void StopStageMove();
}
