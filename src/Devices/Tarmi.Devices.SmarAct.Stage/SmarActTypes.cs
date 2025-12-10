namespace Tarmi.Devices.SmarAct.Stage;

public enum ResponseType
{
    // SCPI 
    NoError             =    0,
    InvalidCharacter    = -101,
    InvalidSeparator    = -103,
    DataTypeError       = -104,
    ParameterNotAllowed = -108,
    MissingParameter    = -109,
    UndefinedHeader     = -113,
    InvalidStringData   = -151,
    QueueOverflow       = -350,
    InputBufferOverrun  = -363,
    // MCS2
    Acknowledgement   =  0,
    UnknownCommand    =  1,
    InvalidPacketSize =  2,
    Timeout           =  4,
    InvalidProtocol   =  5,
    BufferUnderflow   = 12,
    BufferOverflow    = 13,
    InvalidFrameSize  = 14,
    InvalidPacket     = 16,
    InvalidKey        = 18,
    InvalidParameter  = 19,
    InvalidDataType   = 22,
    InvalidData       = 23,
    // TODO: Add missing error codes
}

[Flags]
public enum DeviceState
{
    HandModulePresent            = 0x0001,
    MovementLocked               = 0x0002,
    AmplifierLocked              = 0x0004,
    IOModuleInput                = 0x0008,
    GlobalInput                  = 0x0010,
    InternalCommunicationFailure = 0x0100,
    IsStreaming                  = 0x1000,
}

[Flags]
public enum ModuleState
{
    SensorModulePresent          = 0x0001,
    BoosterPresent               = 0x0002,
    AdjustmentActive             = 0x0004,
    IOModulePresent              = 0x0008,
    InternalCommunicationFailure = 0x0100,
    FanFailure                   = 0x0800,
    PowerSupplyFailure           = 0x1000,
    HighVoltageFailure           = 0x1000,
    PowerSupplyOverload          = 0x2000,
    HighVoltageOverload          = 0x2000,
    OverTemperature              = 0x4000,
}

[Flags]
public enum ChannelState
{
    Idle                  = 0x0,
    ActivelyMoving        = 0x00001,
    ClosedLoopActive      = 0x00002,
    Calibrating           = 0x00004,
    Referencing           = 0x00008,
    MoveDelayed           = 0x00010,
    SensorPresent         = 0x00020,
    IsCalibrated          = 0x00040,
    IsReferenced          = 0x00080,
    EndStopReached        = 0x00100,
    RangeLimitReached     = 0x00200,
    FollowingLimitReached = 0x00400,
    MovementFailed        = 0x00800,
    IsStreaming           = 0x01000,
    PositionerOverload    = 0x02000,
    OverTemperature       = 0x04000,
    ReferenceMark         = 0x08000,
    IsPhased              = 0x10000,
    PositionerFault       = 0x20000,
    AmplifierEnabled      = 0x40000,
    InPosition            = 0x80000
}

// Taken from https://www.envox.eu/bench-power-supply/psu-scpi-reference-manual/psu-scpi-registers-and-queues/
[Flags]
public enum SCPIStatus : byte
{
    Error                   = 0x04,
    Questionable            = 0x08,
    MessageAvailable        = 0x10,
    StandardEvent           = 0x20,
    MasterStatusSummary     = 0x40,
    OperationStatusRegister = 0x80,
}

public enum MovementMode : byte
{
    ClosedLoopAbsolute,
    ClosedLoopRelative,
    ScanAbsolute,
    ScanRelative,
    Step
}
