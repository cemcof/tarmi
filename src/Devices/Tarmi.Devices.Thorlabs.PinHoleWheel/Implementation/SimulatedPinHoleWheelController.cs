using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;
using Tarmi.Configuration.Alignments;

namespace Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;

public class SimulatedPinHoleWheelController : IPinHoleWheelController
{
    public long Position => _position;
    public bool PinHoleWheelIsActive => _pinHoleWheelIsActive;

    public int Address => _address;

    private readonly ILogger _logger;
    private readonly int AnswerDelay = 300;
    private readonly Length _minPinHoleSize;
    private readonly Length _maxPinHoleSize;

    private bool _pinHoleWheelIsActive = false;
    private long _position = 0;
    private int _address = 1;

    public SimulatedPinHoleWheelController(PinHoleWheelAlignments alignments, ILogger<PinHoleWheelController> logger)
    {
        _logger = logger;
        _minPinHoleSize = alignments.PinHoleAlignments.First().PinHoleSize;
        _maxPinHoleSize = alignments.PinHoleAlignments.Last().PinHoleSize;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void SetDeviceAddress(int address) => _address = address;

    public async Task<bool> IsDeviceActive(CancellationToken cancellationToken = default)
    {
        var result = "EZStepper AllMotion";
        _pinHoleWheelIsActive = string.Equals(result, "EZStepper AllMotion", StringComparison.InvariantCultureIgnoreCase);
        await Task.Delay(AnswerDelay, cancellationToken);
        return _pinHoleWheelIsActive;
    }

    public async Task<long> GetCurrentPosition(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulator getting current position.");
        // expected return /0%dR<CR>, example : /0(0x00000143)R\r
        await Task.Delay(AnswerDelay, cancellationToken);
        return Position;
    }

    public async Task<bool> SetPosition(long position, CancellationToken cancellationToken = default)
    {
        Guard.IsBetweenOrEqualTo(Length.FromNanometers(position), _minPinHoleSize, _maxPinHoleSize);

        _logger.LogInformation("Simulator setting new position to {Position}.", position);
        // expected return /0okR<CR>
        var response = "ok";
        await Task.Delay(AnswerDelay, cancellationToken);

        if (string.Equals(response, "ok", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogTrace("Simulator new position was successfully set.");
            _position = position;
            return true;
        }
        else
        {
            _logger.LogError("Simulator new position wasn't set. Response was {Response}.", response);
            return false;
        }
    }
}
