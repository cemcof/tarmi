using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration.Alignments;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;

public class PinHoleWheelController : IPinHoleWheelController
{
    public long Position => _position;
    public bool PinHoleWheelIsActive => _pinHoleWheelIsActive;

    public int Address => _address;

    private readonly ILogger _logger;
    private readonly ISerialCommunication _serialCommunication;
    private readonly Length _minPinHoleSize;
    private readonly Length _maxPinHoleSize;

    private bool _pinHoleWheelIsActive = false;
    private long _position;
    private int _address = 1;

    public PinHoleWheelController(ISerialCommunication serialCommunication, PinHoleWheelAlignments alignments, ILogger<PinHoleWheelController> logger)
    {
        _serialCommunication = serialCommunication;
        _logger = logger;
        _minPinHoleSize = alignments.PinHoleAlignments.First().PinHoleSize;
        _maxPinHoleSize = alignments.PinHoleAlignments.Last().PinHoleSize;
    }

    public void Dispose() => _serialCommunication.Dispose();

    public void SetDeviceAddress(int address) => _address = address;

    public async Task<bool> IsDeviceActive(CancellationToken cancellationToken = default)
    {
        var command = Commands.GetCurrentStepperModeCommand(_address);
        var result = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        _pinHoleWheelIsActive = string.Equals(result, "EZStepper AllMotion", StringComparison.InvariantCultureIgnoreCase);
        return _pinHoleWheelIsActive;
    }

    public async Task<long> GetCurrentPosition(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting current position.");
        var command = Commands.GetCurrentPositionCommand(_address);
        var result = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        // expected return /0%dR<CR>, example : /0(0x00000143)R\r
        _position = int.Parse(result.Substring(5, 8), System.Globalization.NumberStyles.HexNumber);
        return Position;
    }

    public async Task<bool> SetPosition(long position, CancellationToken cancellationToken = default)
    {
        Guard.IsBetweenOrEqualTo(Length.FromNanometers(position), _minPinHoleSize, _maxPinHoleSize);

        _logger.LogInformation("Setting new position to {Position}.", position);
        var command = Commands.SetGoToPositionCommand(_address, position);
        var result = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        // expected return /0okR<CR>
        var response = result[2..result.IndexOf('R')];

        if (string.Equals(response, "ok", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogTrace("New position was successfully set.");
            _position = position;
            return true;
        }
        else
        {
            _logger.LogError("New position wasn't set. Response was {Response}.", response);
            return false;
        }
    }
}
