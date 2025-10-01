using UnitsNet;

namespace Betrian.Devices.SmarAct.Stage.Implementation;

internal static class TypesMappingExtensions
{
    private static readonly Duration Second = Duration.FromSeconds(1);

    public static double ToPicometersPerSecond(this Speed speed) => (speed * Second).Picometers;
    public static double ToPicometersPerSecondSquared(this Acceleration acceleration) => (acceleration * Second * Second).Picometers;
}
