using Tarmi.Imaging.Common.Metadata.Luminescence;
using AwesomeAssertions;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.Metadata.Luminescence;
public class LuminescenceMetadataSerializationTests
{
    [Fact]
    public void Serialization_of_known_xml_should_succeed()
    {
        const string xml =
        """
        <?xml version="1.0" encoding="utf-16"?>
        <Metadata xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/Tarmi.Imaging.Common.Metadata.Luminescence">
          <Camera>
            <Binning>1</Binning>
            <BinningMode>Average</BinningMode>
            <BlackLevel>0</BlackLevel>
            <ExposureTime>PT0S</ExposureTime>
            <FrameRateHz>0</FrameRateHz>
            <Gamma>0</Gamma>
            <Gain>0</Gain>
          </Camera>
          <LightFrequency xmlns:d2p1="http://schemas.datacontract.org/2004/07/UnitsNet">
            <d2p1:Value>0</d2p1:Value>
            <d2p1:Unit>Terahertz</d2p1:Unit>
          </LightFrequency>
          <LightIntensity xmlns:d2p1="http://schemas.datacontract.org/2004/07/UnitsNet">
            <d2p1:Value>0</d2p1:Value>
            <d2p1:Unit>DecimalFraction</d2p1:Unit>
          </LightIntensity>
          <PixelSizeX xmlns:d2p1="http://schemas.datacontract.org/2004/07/UnitsNet">
            <d2p1:Value>0</d2p1:Value>
            <d2p1:Unit>Meter</d2p1:Unit>
          </PixelSizeX>
          <PixelSizeY xmlns:d2p1="http://schemas.datacontract.org/2004/07/UnitsNet">
            <d2p1:Value>0</d2p1:Value>
            <d2p1:Unit>Meter</d2p1:Unit>
          </PixelSizeY>
          <WorkingDistance xmlns:d2p1="http://schemas.datacontract.org/2004/07/UnitsNet">
            <d2p1:Value>0</d2p1:Value>
            <d2p1:Unit>Meter</d2p1:Unit>
          </WorkingDistance>
        </Metadata>
        """;

        var metadata = MetadataXmlSerializer.Deserialize(xml);

        _ = metadata.Should().NotBeNull();
        _ = metadata.Camera.Should().NotBeNull();
        _ = metadata.StackInfo.Should().BeNull();
    }
}
