namespace Betrian.Communication.Common.Serial;

public interface ISerialCommunicationFactory
{
    ISerialCommunication CreateSerialCommunication(SerialPortConfiguration configuration);
}
