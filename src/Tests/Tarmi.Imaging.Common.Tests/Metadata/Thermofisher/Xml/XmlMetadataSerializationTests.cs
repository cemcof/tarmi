using Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;
using AwesomeAssertions;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.Metadata.Thermofisher.Xml;

public class XmlMetadataSerializationTests
{
    [Fact]
    public void Serialization_of_known_xml_should_succeed()
    {
        const string xml =
"""
<?xml version="1.0"?>
<Metadata xmlns:nil="http://schemas.fei.com/Metadata/v1/2013/07" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Core>
    <Guid>e2725362-40be-4551-ad2f-81be4131f500</Guid>
    <UserID>user</UserID>
    <ApplicationSoftware>xT</ApplicationSoftware>
    <ApplicationSoftwareVersion>0</ApplicationSoftwareVersion>
  </Core>
  <Instrument>
    <ControlSoftwareVersion>17.21.0.16195</ControlSoftwareVersion>
    <Manufacturer>FEI Company</Manufacturer>
    <InstrumentClass>Helios 5 Hydra CX</InstrumentClass>
    <InstrumentID>9954108</InstrumentID>
    <ComputerName>HDA12V-MPC</ComputerName>
  </Instrument>
  <Acquisition>
    <AcquisitionDatetime>2022-10-04T08:41:33</AcquisitionDatetime>
    <BeamType>Electron</BeamType>
    <ColumnType>Elstar</ColumnType>
    <SourceType>FEG</SourceType>
  </Acquisition>
  <Optics>
    <Apertures>
      <Aperture>
        <Number>1</Number>
        <Diameter>3.2E-05</Diameter>
      </Aperture>
    </Apertures>
    <GunTiltRaw>
      <X>-0.00520833333333337</X>
      <Y>0.0049999999999998934</Y>
    </GunTiltRaw>
    <AccelerationVoltage>2000</AccelerationVoltage>
    <SpotSize>6.95684106955009E-09</SpotSize>
    <BeamCurrent>5E-11</BeamCurrent>
    <FullScanFieldOfView>
      <X>0.00055253333333333328</X>
      <Y>0.0004769916666666666</Y>
    </FullScanFieldOfView>
    <ScanFieldOfView>
      <X>0.00055253333333333328</X>
      <Y>0.0004769916666666666</Y>
    </ScanFieldOfView>
    <WorkingDistance>0.003798480184333642</WorkingDistance>
    <EucentricWorkingDistance>0.003976087452633165</EucentricWorkingDistance>
    <BeamShift>
      <X>0</X>
      <Y>-0</Y>
    </BeamShift>
    <SampleTiltCorrectionOn>false</SampleTiltCorrectionOn>
    <SamplePreTiltAngle>-0.663225115757845</SamplePreTiltAngle>
    <StigmatorRaw>
      <X>7.26415455565288E-18</X>
      <Y>-0.00095181734094157433</Y>
    </StigmatorRaw>
    <OpticalMode>Field-Free</OpticalMode>
    <CrossOverOn>false</CrossOverOn>
  </Optics>
  <StageSettings>
    <StagePosition>
      <X>-0.0044612436205052064</X>
      <Y>0.0043247916666666695</Y>
      <Z>0.03104902906378601</Z>
      <Rotation>-3.1375848346345232</Rotation>
      <Tilt>
        <Alpha>0.47122794094908188</Alpha>
        <Beta>0</Beta>
      </Tilt>
    </StagePosition>
  </StageSettings>
  <ScanSettings>
    <DwellTime>3E-07</DwellTime>
    <ScanSize>
      <Width>2048</Width>
      <Height>1768</Height>
    </ScanSize>
    <ScanArea>
      <X>0</X>
      <Y>0</Y>
      <Width>2048</Width>
      <Height>1768</Height>
    </ScanArea>
    <MainsLockOn>true</MainsLockOn>
    <LineTime>0.0006876</LineTime>
    <LineIntegrationCount>1</LineIntegrationCount>
    <LineInterlacing>1</LineInterlacing>
    <FrameTime>1.2156768</FrameTime>
    <ScanRotation>3.1415926535897931</ScanRotation>
  </ScanSettings>
  <VacuumProperties>
    <SamplePressure>6.4099999999999363E-05</SamplePressure>
    <ElectronColumnPressure>1.864408477558754E-05</ElectronColumnPressure>
    <IonColumnPressure>3.7099316090638593E-05</IonColumnPressure>
  </VacuumProperties>
  <Detectors>
    <ScanningDetector>
      <DetectorName>ETD</DetectorName>
      <DetectorType>ETD</DetectorType>
      <Signal>SE</Signal>
      <Gain>22.958770005757348</Gain>
      <Offset>-2.1695764807585469</Offset>
      <GridVoltage>600</GridVoltage>
      <SuctionTubeVoltage>-10</SuctionTubeVoltage>
      <ContrastNormalized>59.508440855658627</ContrastNormalized>
      <BrightnessNormalized>40.960723007871614</BrightnessNormalized>
    </ScanningDetector>
  </Detectors>
  <GasInjectionSystems>
    <Gis>
      <PortName>Port1</PortName>
      <NeedleState>Retracted</NeedleState>
      <Gases>
        <Gas>
          <GasType>G1</GasType>
        </Gas>
      </Gases>
    </Gis>
    <Gis>
      <PortName>Port2</PortName>
      <NeedleState>Retracted</NeedleState>
      <Gases>
        <Gas>
          <GasType>G2</GasType>
        </Gas>
      </Gases>
    </Gis>
  </GasInjectionSystems>
  <BinaryResult>
    <AcquisitionUnit>Pixel</AcquisitionUnit>
    <CompositionType>Single</CompositionType>
    <ImageSize>
      <X>2048</X>
      <Y>1768</Y>
    </ImageSize>
    <FilterType>DriftCorrectedFrameIntegration</FilterType>
    <FilterFrameCount>1</FilterFrameCount>
    <PixelSize>
      <X unit="m" unitPrefixPower="1">2.6979166666666664E-07</X>
      <Y unit="m" unitPrefixPower="1">2.6979166666666664E-07</Y>
    </PixelSize>
    <IntensityScale>1</IntensityScale>
    <IntensityOffset>0</IntensityOffset>
    <Gamma>1</Gamma>
    <AcquisitionArea>
      <X>0</X>
      <Y>0</Y>
      <Width>1</Width>
      <Height>1</Height>
    </AcquisitionArea>
  </BinaryResult>
  <CustomPropertyGroup>
    <CustomProperties scope="Magnification Calibration">
      <CustomProperty name="IsOn" value="False" type="System.Boolean" rawdatatype="Boolean" />
      <CustomProperty name="Magnification.X" value="1" type="System.Double" rawdatatype="Double" />
      <CustomProperty name="Magnification.Y" value="1" type="System.Double" rawdatatype="Double" />
      <CustomProperty name="Orthogonality" value="0" type="System.Double" rawdatatype="Double" />
      <CustomProperty name="Rotation" value="0" type="System.Double" rawdatatype="Double" />
      <CustomProperty name="Magnification Index" value="2" type="System.Int32" rawdatatype="Int" />
      <CustomProperty name="Align Angle" value="0.720307596275586" type="System.Double" rawdatatype="Double" />
    </CustomProperties>
  </CustomPropertyGroup>
</Metadata>
""";

        // TODO: CustomPropertyGroup is not deserialized correctly
        var metadata = MetadataXmlSerializer.Deserialize(xml);

        metadata.Should().NotBeNull();
        metadata.Instrument.Should().NotBeNull();
        metadata.Instrument?.Manufacturer.Should().Be("FEI Company");

        var newXml = MetadataXmlSerializer.Serialize(metadata);

    }
}
