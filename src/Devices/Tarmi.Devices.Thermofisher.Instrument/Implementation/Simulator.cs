using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument.Implementation;

internal static class Simulator
{
    // TODO: split to sem and ion?
    public static class Beam
    {
        public static class Limits
        {
            public static LengthRangeDescriptorWithStep FreeWorkingDistance { get; } = new()
            {
                Min = Length.FromMeters(0.0005),
                Max = Length.FromMeters(0.07),
                Step = Length.FromMeters(0.0001)
            };

            public static LengthRangeDescriptor HorizontalFieldWidthRange { get; } = new()
            {
                Min = Length.FromMeters(6E-08),
                Max = Length.FromMeters(0.0028)
            };

            public static RangeDescriptor<int> BeamCurrentIndex { get; } = new()
            {
                Min = 1,
                Max = 14
            };

            public static RangeDescriptor<Duration> DwellTime { get; } = new()
            {
                Min = Duration.FromSeconds(2.5E-08),
                Max = Duration.FromSeconds(107.37408)
            };

            public static RangeDescriptor<double> SpotSize { get; } = new()
            {
                Min = -9.0,
                Max = 7.0
            };
        }

        public static class InitialValues
        {
            public static bool IsOn { get; } = false;
            public static bool IsBlanked { get; } = false;
            public static int BeamCurrentIndex { get; } = 3;
            public static LengthPoint BeamShift { get; } = LengthPoint.Zero;
            public static Duration DwellTime { get; } = Duration.FromNanoseconds(100);
            public static LengthPoint Stigmator { get; } = LengthPoint.Zero;
            public static Length FreeWorkingDistance { get; } = Length.FromMeters(0.00358);
            public static ElectricPotential HV { get; } = ElectricPotential.FromVolts(500);
            public static Angle ScanRotation { get; } = Angle.FromDegrees(0);
            public static double SpotSize { get; } = -2.0;
            public static Length HorizontalFieldWidth { get; } = Length.FromMeters(0.001);
            public static Length VerticalFieldWidth { get; } = HorizontalFieldWidth / 3 * 2;

            public static Resolution Resolution { get; } = new Resolution
            {
                Width = 1024,
                Height = 768,
                Depth = Resolution.Mono8Depth
            };

            public static LengthPoint PixelSize { get; } = new LengthPoint
            {
                X = Length.FromMeters(HorizontalFieldWidth.Meters / Resolution.Width),
                Y = Length.FromMeters(VerticalFieldWidth.Meters / Resolution.Height)
            };

            public static ElectricCurrent[] BeamCurrents { get; } =
            [
                ElectricCurrent.FromAmperes(1E-13),
                ElectricCurrent.FromAmperes(1E-12),
                ElectricCurrent.FromAmperes(3E-12),
                ElectricCurrent.FromAmperes(1E-11),
                ElectricCurrent.FromAmperes(3E-11),
                ElectricCurrent.FromAmperes(1E-10),
                ElectricCurrent.FromAmperes(3E-10),
                ElectricCurrent.FromAmperes(1E-09),
                ElectricCurrent.FromAmperes(4E-09),
                ElectricCurrent.FromAmperes(1.5E-08),
                ElectricCurrent.FromAmperes(6E-08),
                ElectricCurrent.FromAmperes(2E-07),
                ElectricCurrent.FromAmperes(5E-07),
                ElectricCurrent.FromAmperes(1E-06),
                ElectricCurrent.FromAmperes(2.5E-06),
            ];
        }
    }

    public static class Stage
    {
        public static class Limits
        {
            public static StageLimits Axes { get; } = new()
            {
                X = new LengthRangeDescriptor
                {
                    Min = Length.FromMillimeters(-55),
                    Max = Length.FromMillimeters(55)
                },
                Y = new LengthRangeDescriptor
                {
                    Min = Length.FromMillimeters(-55),
                    Max = Length.FromMillimeters(55)
                },
                Z = new LengthRangeDescriptor
                {
                    Min = Length.FromMillimeters(-0.25),
                    Max = Length.FromMillimeters(66.006669701)
                },
                Rotation = new AngleRangeDescriptor
                {
                    Min = Angle.FromDegrees(0),
                    Max = Angle.FromDegrees(0),
                },
                Tilt = new AngleRangeDescriptor
                {
                    Min = Angle.FromDegrees(-10),
                    Max = Angle.FromDegrees(90),
                }
            };
        }

        public static class InitialValues
        {
            public static StagePosition CurrentPosition { get; } = new()
            {
                X = Length.FromMeters(0),
                Y = Length.FromMeters(0),
                Z = Length.FromMeters(0.0045),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(0)
            };

            public static bool IsLinked { get; } = true;
            public static bool IsInError { get; } = false;
            public static bool IsMoving { get; } = false;
        }
    }

    public static class Chamber
    {
        public static class InitialValues
        {
            public static Pressure Pressure { get; } = Pressure.FromPascals(5.28e-05);
        }
    }
}
