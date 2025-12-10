namespace Tarmi.Communication.Common.Serial;

public interface ISerialCommunicationFactory
{
    ISerialCommunication CreateSerialCommunication(SerialPortConfiguration configuration);
}
