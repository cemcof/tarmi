using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Betrian.Communication.Common.Serial.Implementation;
internal static class SerialPortExtensions
{
    public static async Task WriteLineAsync(this SerialPort serialPort, string text, CancellationToken cancellationToken = default)
    {
        text = $"{text}{serialPort.NewLine}";
        await serialPort.WriteAsync(text, cancellationToken);
    }

    public static async Task WriteAsync(this SerialPort serialPort, string text, CancellationToken cancellationToken = default)
    {
        await using var streamWriter = new StreamWriter(serialPort.BaseStream, serialPort.Encoding, leaveOpen: true);
        await streamWriter.WriteAsync(text.AsMemory(), cancellationToken);
    }

    public static async Task<string> ReadLineAsync(this SerialPort serialPort, CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(serialPort.BaseStream, serialPort.Encoding, leaveOpen: true);
        return await streamReader.ReadLineAsync(cancellationToken) ?? string.Empty;
    }
}
