namespace Betrian.CflmNavi.App.ViewModels.ROIs;

[Flags]
public enum ImageAttributes
{
    None = 0x00,
    Reference = 0x01,
    SEM = 0x02,
    FIB = 0x04,
    Reflection = 0x08,
    Luminescence = 0x10,
    Red = 0x20,
    Green = 0x40,
    Blue = 0x80,
    UltraViolet = 0x100,
    TileSet = 0x200,
    ZStack = 0x400,
    MipImage = 0x800,
    Folder = 0x1000,
}
