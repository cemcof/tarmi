using Tarmi.Imaging.Algorithms.Utilities;
using AwesomeAssertions;
using UnitsNet;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.ImageAlgorithms;

public class LightConversions
{
    public class TestData : TheoryData<Length>
    {
        public TestData()
        {
            Add(Length.FromNanometers(625));
            Add(Length.FromNanometers(565));
            Add(Length.FromNanometers(470));
            Add(Length.FromNanometers(385));
        }
    }

    [Theory]
    [ClassData(typeof(TestData))]
    public void Wavelength_Frequency_Conversions_Should_Be_Reliable(Length wavelength)
    {
        var frequency = ColorSpace.WavelengthToFrequency(wavelength);
        var convertedWavelength = ColorSpace.FrequencyToWavelength(frequency);
        _ = wavelength.Nanometers.Should().BeApproximately(convertedWavelength.Nanometers, 0.000_000_001);
    }
}
