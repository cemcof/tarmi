namespace Tarmi.Communication.Common.Serial;

public class SerialPortConfiguration
{
    public required string PortName { get; init; }
    public int BaudRate { get; init; } = 9600;
    public string NewLine { get; init; } = Environment.NewLine;

    public static implicit operator Tarmi.Configuration.Devices.SerialPort(SerialPortConfiguration configuration) => new()
    {
        DeviceName = configuration.PortName,
        BaudRate = configuration.BaudRate,
        NewLineData = SimpleBase.Base16.UpperCase.Encode(System.Text.Encoding.ASCII.GetBytes(configuration.NewLine))
    };

    public static implicit operator SerialPortConfiguration(Tarmi.Configuration.Devices.SerialPort port) => new()
    {
        PortName = port.DeviceName,
        BaudRate = port.BaudRate,
        NewLine = port.NewLine
    };
}
