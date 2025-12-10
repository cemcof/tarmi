using System.Text;
using System.Xml.Serialization;
using Tarmi.Maps.DataFormat.TfsDataModel;

namespace Tarmi.Maps.DataFormat;

public class MapsExport
{
    private const string MapsExportFileName = "mapsExport.tfs.xml";
    public string ExportPath { get; init; }
    public string ImagesPath { get; init; }
    public string ManualImportPath {  get; init; }

    private static XmlSerializerNamespaces CreateSerializerNamespaces()
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("maps", "http://www.thermofisher.com/schemas/maps");
        return namespaces;
    }

    public MapsExport(string baseExportDirectory)
    {
        _ = Directory.CreateDirectory(baseExportDirectory);

        string newExportDirectory = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        ExportPath = Path.Combine(baseExportDirectory, newExportDirectory);
        _ = Directory.CreateDirectory(ExportPath);
        ManualImportPath = Path.Combine(ExportPath, "ManualImportImages");
        ImagesPath = Path.Combine(ExportPath, "Images");
    }

    public void XmlExport(TfsData tfsData)
    {
        var xmlSerializer = new XmlSerializer(typeof(TfsData));

        using (StreamWriter writer = new(
            Path.Combine(ExportPath, MapsExportFileName),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            new FileStreamOptions() { Mode = FileMode.CreateNew, Access = FileAccess.Write }))
        {
            xmlSerializer.Serialize(writer, tfsData, CreateSerializerNamespaces());
        }
    }
    
}
