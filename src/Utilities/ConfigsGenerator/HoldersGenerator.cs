using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Holders;
using UnitsNet;

internal static class HoldersConfigGenerator
{
    public static void Generate(string filename = "holders.xml")
    {
        var config = new HoldersConfig()
        {
            Holders =
            [
                CreateAgAgHolder(),
                CreateAgAg35Holder(),
                CreateP3AgHolder(),
                CreateP6AgHolder(),
                //CreateAgAg35Hydra2Holder(),
            ]
        };

        ConfigSerialization.SaveHoldersConfig(config, filename);
    }

    private static Holder CreateAgAgHolder()
    {
        return new()
        {
            Name = "Holder AG-AG",
            PreTilt = Angle.FromDegrees(27),
            SemModePlanePoint = new()
            {
                X = Length.FromMeters(0.0033687),
                Y = Length.FromMeters(0.0031086),
                Z = Length.FromMeters(0.0315889),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(27)
            },
            FibMillingModePlanePoint = new()
            {
                X = Length.FromMeters(0.0033687),
                Y = Length.FromMeters(0.0031086),
                Z = Length.FromMeters(0.0315889),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(27)
            },
            FibRightAngleModePlanePoint = new()
            {
                X = Length.FromMeters(0.0033687),
                Y = Length.FromMeters(-0.0031086),
                Z = Length.FromMeters(0.0315889),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(25)
            },
            LmModePlanePoint = new()
            {
                X = Length.FromMeters(0.0488916),
                Y = Length.FromMeters(-0.0025213),
                Z = Length.FromMeters(0.0312001),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(25.4) // 25 expected, delta?
            },
            SafeUnknownMoveZ = Length.FromMillimeters(20),
            SafeTiltRange = new Betrian.Models.AngleRangeDescriptor()
            {
                Min = Angle.FromDegrees(-2),
                Max = Angle.FromDegrees(2)
            },
            Grids =
            [
                new CircleAreaOfInterest()
                {
                    Name = "AG 1",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(2.720),
                        Y = Length.FromMillimeters(0.134),
                    }
                },
                new CircleAreaOfInterest()
                {
                    Name = "AG 2",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(-3.341),
                        Y = Length.FromMillimeters(0.320),
                    }
                }
            ]
        };
    }

    private static Holder CreateAgAg35Holder()
    {
        return new()
        {
            //G1
            //SEM -2.6310mm, 3.2918mm, 32.0266mm, 35, 180
            //FIB -2.6310mm, 3.2918mm, 32.0266mm, 35, 180
            //FIB RA 2.6241mm, -3.0846mm, 31.5641mm, 17, 0
            //LM 54.7605mm, -2.5584mm, 31.2006mm, 17.4, 0

            //G2
            //SEM 3.2855mm, 3.3382mm, 32.0262mm, 35, 180
            //FIB 3.2855mm, 3.3382mm, 32.0262mm, 35, 180
            //FIB RA -3.1797mm, -3.0902mm, 31.5644mm, 17, 0
            //LM 48.9606mm, -2.5584mm, 31.2006mm, 17.4, 0

            Name = "Holder AG-AG 35",
            PreTilt = Angle.FromDegrees(35),
            SemModePlanePoint = new()
            {
                X = Length.FromMillimeters(-2.660),
                Y = Length.FromMillimeters(3.347),
                Z = Length.FromMillimeters(32.020),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibMillingModePlanePoint = new()
            {
                X = Length.FromMillimeters(3.2855),
                Y = Length.FromMillimeters(3.3382),
                Z = Length.FromMillimeters(32.0262),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibRightAngleModePlanePoint = new()
            {
                X = Length.FromMillimeters(2.773),
                Y = Length.FromMillimeters(-3.155),
                Z = Length.FromMillimeters(31.639),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17)
            },
            LmModePlanePoint = new()
            {
                X = Length.FromMillimeters(54.533),
                Y = Length.FromMillimeters(-2.215),
                Z = Length.FromMillimeters(31.442),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17.4)
            },
            SafeUnknownMoveZ = Length.FromMillimeters(20),
            SafeTiltRange = new Betrian.Models.AngleRangeDescriptor()
            {
                Min = Angle.FromDegrees(-2),
                Max = Angle.FromDegrees(2)
            },
            Grids =
            [
                new CircleAreaOfInterest()
                {
                    Name = "AG 1",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(2.2660),
                        Y = Length.FromMillimeters(-0.011),
                        //X = Length.FromMillimeters(-1.258),
                        //Y = Length.FromMillimeters(0.000),
                    }
                },
                new CircleAreaOfInterest()
                {
                    Name = "AG 2",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(-3.399),
                        Y = Length.FromMillimeters(0.214),
                        //X = Length.FromMillimeters(4.542),
                        //Y = Length.FromMillimeters(0.000),
                    }
                }
            ]
        };
    }

    private static Holder CreateAgAg35Hydra2Holder()
    {
        return new()
        {
            Name = "Holder AG-AG 35 HYDRA 2",
            PreTilt = Angle.FromDegrees(35),
            SemModePlanePoint = new()
            {
                X = Length.FromMillimeters(-2.7147),
                Y = Length.FromMillimeters(2.5754),
                Z = Length.FromMillimeters(31.7710),
                Rotation = Angle.FromDegrees(-70),
                Tilt = Angle.FromDegrees(35)
            },
            FibMillingModePlanePoint = new()
            {
                X = Length.FromMillimeters(-2.7147),
                Y = Length.FromMillimeters(2.5754),
                Z = Length.FromMillimeters(31.7710),
                Rotation = Angle.FromDegrees(-70),
                Tilt = Angle.FromDegrees(35)
            },
            FibRightAngleModePlanePoint = new()
            {
                X = Length.FromMillimeters(2.8087),
                Y = Length.FromMillimeters(-2.7815),
                Z = Length.FromMillimeters(31.7703),
                Rotation = Angle.FromDegrees(110),
                Tilt = Angle.FromDegrees(17)
            },
            LmModePlanePoint = new()
            {
                // !!!
                X = Length.FromMillimeters(48.9606),
                Y = Length.FromMillimeters(-2.5584),
                Z = Length.FromMillimeters(31.2006),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17.4)
            },
            SafeUnknownMoveZ = Length.FromMillimeters(20),
            SafeTiltRange = new Betrian.Models.AngleRangeDescriptor()
            {
                Min = Angle.FromDegrees(-2),
                Max = Angle.FromDegrees(2)
            },
            Grids =
            [
                new CircleAreaOfInterest()
                {
                    Name = "AG 1",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(2.720),
                        Y = Length.FromMillimeters(0.133),
                    }
                },
                new CircleAreaOfInterest()
                {
                    Name = "AG 2",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(-3.275),
                        Y = Length.FromMillimeters(-0.193),
                    }
                }
            ]
        };
    }

    private static Holder CreateP3AgHolder()
    {
        return new()
        {
            Name = "Holder P3-AG",
            PreTilt = Angle.FromDegrees(35),
            SemModePlanePoint = new()
            {
                X = Length.FromMillimeters(-2.660),
                Y = Length.FromMillimeters(3.347),
                Z = Length.FromMillimeters(32.020),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibMillingModePlanePoint = new()
            {
                X = Length.FromMillimeters(3.2855),
                Y = Length.FromMillimeters(3.3382),
                Z = Length.FromMillimeters(32.0262),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibRightAngleModePlanePoint = new()
            {
                X = Length.FromMillimeters(2.773),
                Y = Length.FromMillimeters(-3.155),
                Z = Length.FromMillimeters(31.639),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17)
            },
            LmModePlanePoint = new()
            {
                X = Length.FromMillimeters(54.533),
                Y = Length.FromMillimeters(-2.215),
                Z = Length.FromMillimeters(31.442),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17.4)
            },
            SafeUnknownMoveZ = Length.FromMillimeters(20),
            SafeTiltRange = new Betrian.Models.AngleRangeDescriptor()
            {
                Min = Angle.FromDegrees(-2),
                Max = Angle.FromDegrees(2)
            },
            Grids =
            [
                new CircleAreaOfInterest()
                {
                    Name = "P3",
                    Radius = Length.FromMillimeters(1.2),
                    Center = new() {
                        X = Length.FromMillimeters(2.2660),
                        Y = Length.FromMillimeters(-0.011),
                    }
                },
                new CircleAreaOfInterest()
                {
                    Name = "AG",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new() {
                        X = Length.FromMillimeters(-3.399),
                        Y = Length.FromMillimeters(0.214),
                    }
                }
            ]
        };
    }

    private static Holder CreateP6AgHolder()
    {
        return new()
        {
            Name = "Holder P6-AG",
            PreTilt = Angle.FromDegrees(35),
            SemModePlanePoint = new()
            {
                X = Length.FromMillimeters(-2.660),
                Y = Length.FromMillimeters(3.347),
                Z = Length.FromMillimeters(32.020),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibMillingModePlanePoint = new()
            {
                X = Length.FromMillimeters(3.2855),
                Y = Length.FromMillimeters(3.3382),
                Z = Length.FromMillimeters(32.0262),
                Rotation = Angle.FromDegrees(180),
                Tilt = Angle.FromDegrees(35)
            },
            FibRightAngleModePlanePoint = new()
            {
                X = Length.FromMillimeters(2.773),
                Y = Length.FromMillimeters(-3.155),
                Z = Length.FromMillimeters(31.639),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17)
            },
            LmModePlanePoint = new()
            {
                X = Length.FromMillimeters(54.533),
                Y = Length.FromMillimeters(-2.215),
                Z = Length.FromMillimeters(31.442),
                Rotation = Angle.FromDegrees(0),
                Tilt = Angle.FromDegrees(17.4)
            },
            SafeUnknownMoveZ = Length.FromMillimeters(20),
            SafeTiltRange = new Betrian.Models.AngleRangeDescriptor()
            {
                Min = Angle.FromDegrees(-2),
                Max = Angle.FromDegrees(2)
            },
            Grids =
            [
                new CircleAreaOfInterest()
                {
                    Name = "P6",
                    Radius = Length.FromMillimeters(2.8),
                    Center = new()
                    {
                        X = Length.FromMillimeters(2.2660),
                        Y = Length.FromMillimeters(-0.011),
                    }
                },
                new CircleAreaOfInterest()
                {
                    Name = "AG",
                    Radius = Length.FromMillimeters(1.4),
                    Center = new()
                    {
                        X = Length.FromMillimeters(-3.399),
                        Y = Length.FromMillimeters(0.214),
                    }
                }
            ]
        };
    }
}
