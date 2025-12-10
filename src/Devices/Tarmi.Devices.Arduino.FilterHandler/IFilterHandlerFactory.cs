using Tarmi.Communication.Common.Serial;

namespace Tarmi.Devices.Arduino.FilterHandler;

public interface IFilterHandlerFactory
{
    IFilterHandler CreateFilterHandler();
}
