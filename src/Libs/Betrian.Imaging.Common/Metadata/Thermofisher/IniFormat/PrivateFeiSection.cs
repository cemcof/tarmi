namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class PrivateFeiSection
{
    /*
     SAMPLES

    BitShift=0
    DataBarSelected=DateTime HV PW mag HFW det WD MicronBar Label mode tilt
    DataBarAvailable=LensMode srot frame dwell curr WD PW mag HFW x y z tilt rotation filter det DateTime mode driftCorr pressure zoom HV Label MicronBar
    TimeOfCreation=08.03.2024 08:32:18
    DatabarHeight=0

    BitShift=0
    DataBarSelected=
    DataBarAvailable=
    TimeOfCreation=30.06.2023 10:16:12
    DatabarHeight=

    */

    public int BitShift { get; set; }
    public string DataBarSelected { get; set; } = string.Empty;
    public string DataBarAvailable { get; set; } = string.Empty;
    public DateTime TimeOfCreation { get; set; }
    public string DatabarHeight { get; set; } = string.Empty;
}
