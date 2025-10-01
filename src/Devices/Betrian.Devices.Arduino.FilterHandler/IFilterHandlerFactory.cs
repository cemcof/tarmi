using Betrian.Communication.Common.Serial;

namespace Betrian.Devices.Arduino.FilterHandler;

public interface IFilterHandlerFactory
{
    IFilterHandler CreateFilterHandler();
}
