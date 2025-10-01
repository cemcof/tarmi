using CFLMnavi.Configuration;

namespace Betrian.Devices.SmarAct.Stage;

public interface IMcs2CommunicationFactory
{
    public IMcs2Communication CreateCommunication(ApplicationConfig configuration);
}
