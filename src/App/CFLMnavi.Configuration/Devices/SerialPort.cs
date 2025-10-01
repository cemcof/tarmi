using System.Runtime.Serialization;
using Betrian.Models.Serialization;

namespace CFLMnavi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record SerialPort
{
    [DataMember]
    public string DeviceName { get; init; } = string.Empty;

    [DataMember]
    public int BaudRate { get; init; } = 115200;

    [DataMember]
    public string NewLineData { get; init; } = SimpleBase.Base16.UpperCase.Encode([0x0D, 0x0A]);

    [IgnoreDataMember]
    public string NewLine => System.Text.Encoding.ASCII.GetString(SimpleBase.Base16.Decode(NewLineData));
}
