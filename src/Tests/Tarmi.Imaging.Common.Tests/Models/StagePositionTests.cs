using Tarmi.Models;
using AwesomeAssertions;
using UnitsNet;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.Models;

public class StagePositionTests
{
    [Fact]
    public void StagePosition_Equality_Should_Gracefully_Accept_Minimum_Angle_Differences()
    {
        var pos1 = new StagePosition
        {
            X = Length.FromMeters(0.054786),
            Y = Length.FromMeters(-0.002107),
            Z = Length.FromMeters(0.031676),
            Rotation = Angle.FromRadians(-0.000034),
            Tilt = Angle.FromRadians(0.296706)
        };

        var pos2 = new StagePosition
        {
            X = Length.FromMeters(0.054786),
            Y = Length.FromMeters(-0.002107),
            Z = Length.FromMeters(0.031676),
            Rotation = Angle.FromRadians(0.0),
            Tilt = Angle.FromRadians(0.296706)
        };

        var result = pos1.Equals(pos2);
        _ = result.Should().BeTrue();

        pos1 = new StagePosition
        {
            X = Length.FromMeters(0.054786),
            Y = Length.FromMeters(-0.002107),
            Z = Length.FromMeters(0.031676),
            Rotation = Angle.FromRadians(-3.141525),
            Tilt = Angle.FromRadians(0.296706)
        };

        pos2 = new StagePosition
        {
            X = Length.FromMeters(0.054786),
            Y = Length.FromMeters(-0.002107),
            Z = Length.FromMeters(0.031676),
            Rotation = Angle.FromRadians(3.141593),
            Tilt = Angle.FromRadians(0.296706)
        };

        result = pos1.Equals(pos2);
        _ = result.Should().BeTrue();
    }
}
