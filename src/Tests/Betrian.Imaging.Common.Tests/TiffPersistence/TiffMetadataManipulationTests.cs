using Xunit;
using AwesomeAssertions;

namespace Betrian.Imaging.Common.Tests.TiffPersistence;

public class TiffMetadataManipulationTests : IDisposable
{
    private const string ImageFilename = "sem1.tif";
    private const string ModifiedImageFilename = "sem1.modified.tif";

    public void Dispose()
    {
        if (File.Exists(ModifiedImageFilename))
        {
            File.Delete(ModifiedImageFilename);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Modification_of_teff_metadata_should_succeed()
    {
        const string NewApplicationSoftware = "TestApplication";

        var originalImageWithMetadata = TiffImage.Load(ImageFilename);

        var modifiedImageWithMetadata = originalImageWithMetadata with
        {
            FeiXmlMetadata = originalImageWithMetadata.FeiXmlMetadata! with
            {
                Core = originalImageWithMetadata.FeiXmlMetadata.Core with
                {
                    ApplicationSoftware = NewApplicationSoftware
                }
            },
            TiffMetadata = new()
            {
                Software = NewApplicationSoftware,
                ImageDescription = NewApplicationSoftware
            }
        };

        TiffImage.Save(modifiedImageWithMetadata, ModifiedImageFilename);
        var rereadImageWithMetadata = TiffImage.Load(ModifiedImageFilename);

        _ = rereadImageWithMetadata.FeiXmlMetadata!.Core.ApplicationSoftware.Should().Be(NewApplicationSoftware);
        _ = rereadImageWithMetadata.TiffMetadata!.Software.Should().Be(NewApplicationSoftware);
        _ = rereadImageWithMetadata.TiffMetadata!.ImageDescription.Should().Be(NewApplicationSoftware);
    }
}
