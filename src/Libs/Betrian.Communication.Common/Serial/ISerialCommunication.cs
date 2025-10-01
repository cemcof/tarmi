using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Betrian.Communication.Common.Serial;
public interface ISerialCommunication : IDisposable
{
    Task SendCommandAsync(string command, CancellationToken cancellationToken = default);
    Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken = default);
}
