// TODO: check the design
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members

using System.Text;

namespace Tarmi.Devices.SmarAct.Stage.Implementation;

public static class Commands
{
    public static string Commit<T>() where T : ICommitCommandBuilder<T>
        => ICommitCommandBuilder<T>.ToCommitCommand();

    public static string Commit<T>(int index) where T : IIndexedCommitCommandBuilder<T>
        => IIndexedCommitCommandBuilder<T>.ToCommitCommand(index);

    public static string Write<T>(int index, object parameter) where T : IIndexedWriteCommandBuilder<T>
        => IIndexedWriteCommandBuilder<T>.ToWriteCommand(index, parameter);

    public static string Write<T>(object parameter) where T : IWriteCommandBuilder<T>
        => IWriteCommandBuilder<T>.ToWriteCommand(parameter);

    public static string Read<T>(int index) where T : IIndexedReadCommandBuilder<T>
        => IIndexedReadCommandBuilder<T>.ToReadCommand(index);

    public static string Read<T>() where T : IReadCommandBuilder<T>
        => IReadCommandBuilder<T>.ToReadCommand();

    public interface ICommandBuilder
    {
        static abstract StringBuilder GetCommandBuilder();
    }

    public interface IIndexedCommandBuilder
    {
        static abstract StringBuilder GetCommandBuilder(int index);
    }

    public interface ICommitCommandBuilder<TSelf> : ICommandBuilder where TSelf : ICommitCommandBuilder<TSelf>
    {
        public static string ToCommitCommand() => TSelf.GetCommandBuilder().AppendLine().ToString();
    }

    public interface IIndexedCommitCommandBuilder<TSelf> : IIndexedCommandBuilder where TSelf : IIndexedCommitCommandBuilder<TSelf>
    {
        public static string ToCommitCommand(int index) => TSelf.GetCommandBuilder(index).AppendLine().ToString();
    }

    public interface IWriteCommandBuilder<TSelf> : ICommandBuilder where TSelf : IWriteCommandBuilder<TSelf>
    {
        public static string ToWriteCommand(object parameter) => TSelf.GetCommandBuilder().AppendLine($" {parameter}").ToString();
    }

    public interface IReadCommandBuilder<TSelf> : ICommandBuilder
        where TSelf : IReadCommandBuilder<TSelf>
    {
        public static string ToReadCommand() => TSelf.GetCommandBuilder().AppendLine("?").ToString();
    }

    public interface IIndexedReadCommandBuilder<TSelf> : IIndexedCommandBuilder
        where TSelf : IIndexedReadCommandBuilder<TSelf>
    {
        public static string ToReadCommand(int index) => TSelf.GetCommandBuilder(index).AppendLine("?").ToString();
    }

    public interface IIndexedWriteCommandBuilder<TSelf> : IIndexedCommandBuilder where TSelf : IIndexedWriteCommandBuilder<TSelf>
    {
        public static string ToWriteCommand(int index, object parameter) => TSelf.GetCommandBuilder(index).AppendLine($" {parameter}").ToString();
    }

    private static StringBuilder Append<T>(string text) where T : ICommandBuilder => T.GetCommandBuilder().Append(text);
    private static StringBuilder Append<T>(int index, string text) where T : IIndexedCommandBuilder => T.GetCommandBuilder(index).Append(text);

    /// <summary>
    /// Contains SCPI commands implemented by the SmarAct device.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Corresponds to the Clear Status Command.
        /// Should clear all status data structures.
        /// </summary>
        public class ClearStatusCommand : ICommitCommandBuilder<ClearStatusCommand>
        {
            public static StringBuilder GetCommandBuilder() => new("*CLS");
        }

        /// <summary>
        /// Corresponds to the Identification Query.
        /// The response should return information about the device
        /// (manufacturer, serial number, ...).
        /// </summary>
        public class IdentificationCommand : IReadCommandBuilder<IdentificationCommand>
        {
            public static StringBuilder GetCommandBuilder() => new("*IDN");
        }

        /// <summary>
        /// Corresponds to the Reset Command.
        /// Reconnection required after reset.
        /// </summary>
        public class Reset : ICommitCommandBuilder<Reset>
        {
            public static StringBuilder GetCommandBuilder() => new("*RST");
        }

        /// <summary>
        /// Corresponds to the Read Status Byte Query.
        /// The response should be a <see cref="byte"/> compliant with <see cref="SCPIStatus"/>.
        /// </summary>
        public class StatusByte : IReadCommandBuilder<StatusByte>
        {
            public static StringBuilder GetCommandBuilder() => new("*STB");
        }

        /// <summary>
        /// Corresponds to the Self-Test Query.
        /// The response should always be 0.
        /// </summary>
        public class Test : IReadCommandBuilder<Test>
        {
            public static StringBuilder GetCommandBuilder() => new("*TST");
        }
    }

    /// <summary>
    /// Contains commands related to movement.
    /// </summary>
    public static class Movement
    {
        public class Move : IIndexedWriteCommandBuilder<Move>
        {
            public static StringBuilder GetCommandBuilder(int index) => new($":MOVE{index}");
        }

        public class Stop : IIndexedCommitCommandBuilder<Stop>
        {
            public static StringBuilder GetCommandBuilder(int index) => new($":STOP{index}");
        }

        public class Calibrate : IIndexedCommitCommandBuilder<Calibrate>
        {
            public static StringBuilder GetCommandBuilder(int index) => new($":CAL{index}");
        }

        public class Reference : IIndexedCommitCommandBuilder<Reference>
        {
            public static StringBuilder GetCommandBuilder(int index) => new($":REF{index}");
        }
    }

    /// <summary>
    /// Contains commands accessing the configuration properties.
    /// </summary>
    public static class Property
    {
        public class Device : ICommandBuilder
        {
            static StringBuilder ICommandBuilder.GetCommandBuilder() => new(":DEV");

            public class NumberOfChannels : IReadCommandBuilder<NumberOfChannels>
            {
                public static StringBuilder GetCommandBuilder() => Append<Device>(":NOCH");
            }

            /// <summary>
            /// Should return value of form <see cref="SmarActTypes.DeviceStateFlags"/>
            /// </summary>
            public class State : IReadCommandBuilder<State>
            {
                public static StringBuilder GetCommandBuilder() => Append<Device>(":STAT");
            }
        }

        public class System : ICommandBuilder
        {
            public static StringBuilder GetCommandBuilder() => new(":SYST");

            /// <summary>
            /// Removes the first error from the queue and returns it in the form of
            /// <see cref="ResponseType"/> and an error message.
            /// </summary>
            public class Error : IReadCommandBuilder<Error>
            {
                public static StringBuilder GetCommandBuilder() => Append<System>(":ERR");

                /// <summary>
                /// Returns the number of errors present in the error queue.
                /// </summary>
                public class Count : IReadCommandBuilder<Count>
                {
                    public static StringBuilder GetCommandBuilder() => Append<Error>(":COUN");
                }
            }
        }

        public class Channel : IIndexedCommandBuilder
        {
            public static StringBuilder GetCommandBuilder(int index) => new($":CHAN{index}");

            /// <summary>
            /// Should return a value of form <see cref="ChannelState"/>
            /// </summary>
            public class State : IIndexedReadCommandBuilder<State>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":STAT");
            }

            public class Position : IIndexedReadCommandBuilder<Position>, IIndexedWriteCommandBuilder<Position>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":POS");

                public class Target : IIndexedReadCommandBuilder<Target>
                {
                    public static StringBuilder GetCommandBuilder(int index) => Append<Position>(index, ":TARG");
                }
            }

            public class Velocity : IIndexedReadCommandBuilder<Velocity>, IIndexedWriteCommandBuilder<Velocity>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":VEL");
            }

            public class Acceleration : IIndexedReadCommandBuilder<Acceleration>, IIndexedWriteCommandBuilder<Acceleration>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":ACC");
            }

            public class RangeLimit : IIndexedCommandBuilder
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":RLIM");

                public class Minimum : IIndexedReadCommandBuilder<Minimum>
                {
                    public static StringBuilder GetCommandBuilder(int index) => Append<RangeLimit>(index, ":MIN");
                }

                public class Maximum : IIndexedReadCommandBuilder<Maximum>
                {
                    public static StringBuilder GetCommandBuilder(int index) => Append<RangeLimit>(index, ":MAX");
                }
            }

            /// <summary>
            /// Should return a value of form <see cref="Stage.MovementMode"/>
            /// </summary>
            public class MovementMode : IIndexedReadCommandBuilder<MovementMode>, IIndexedWriteCommandBuilder<MovementMode>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":MMOD");
            }

            public class Temperature : IIndexedReadCommandBuilder<Temperature>
            {
                public static StringBuilder GetCommandBuilder(int index) => Append<Channel>(index, ":TEMP");
            }
        }
    }
}
