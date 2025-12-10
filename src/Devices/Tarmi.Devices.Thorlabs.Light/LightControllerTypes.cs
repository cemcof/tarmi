using System.ComponentModel;

namespace Tarmi.Devices.Thorlabs.Light;

public enum LightColor
{
    Red,
    UltraViolet,
    Green,
    Blue
}

public enum OperationMode
{
    ConstantCurrent,
    Brightness,
    ExternalControl
}

public enum SelectionMode
{
    Multi,
    Single
}

[Flags]
public enum StatusCode
{
    None = 0,

    [Description("The bit 'VCC Fail' has changed.")]
    VCCFailChanged = 1 << 0,

    [Description("The power supply is out of range.")]
    VCCFail = 1 << 1,

    [Description("The bit 'OTP' (Over Temperature) has changed.")]
    OTPChange = 1 << 2,

    [Description("Over temperature in the chassis detected. All LEDs switched off.")]
    OTP = 1 << 3,

    [Description("The bit 'No LED1' has changed.")]
    NoLED1Changed = 1 << 4,

    [Description("The LED at channel1 is not connected.")]
    NoLED1 = 1 << 5,

    [Description("The bit 'No LED2' has changed.")]
    NoLED2Changed = 1 << 6,

    [Description("The LED at channel2 is not connected.")]
    NoLED2 = 1 << 7,

    [Description("The bit 'No LED3' has changed.")]
    NoLED3Changed = 1 << 8,

    [Description("The LED at channel3 is not connected.")]
    NoLED3 = 1 << 9,

    [Description("The bit 'No LED4' has changed.")]
    NoLED4Changed = 1 << 10,

    [Description("The LED at channel4 is not connected.")]
    NoLED4 = 1 << 11,

    [Description("The bit 'LED Open1' has changed.")]
    LEDOpen1Changed = 1 << 12,

    [Description("LED channel1: No LED is connected.")]
    LEDOpen1 = 1 << 13,

    [Description("The bit 'LED Open2' has changed.")]
    LEDOpen2Changed = 1 << 14,

    [Description("LED channel2: No LED is connected.")]
    LEDOpen2 = 1 << 15,

    [Description("The bit 'LED Open3' has changed.")]
    LEDOpen3Changed = 1 << 16,

    [Description("LED channel3: No LED is connected.")]
    LEDOpen3 = 1 << 17,

    [Description("The bit 'LED Open4' has changed.")]
    LEDOpen4Changed = 1 << 18,

    [Description("LED channel4: No LED is connected.")]
    LEDOpen4 = 1 << 19,

    [Description("The bit 'Limit1' has changed.")]
    Limit1Changed = 1 << 20,

    [Description("LED channel1: Adjusted current exceeds the current limit and was set to limit.")]
    Limit1 = 1 << 21,

    [Description("The bit 'Limit2' has changed.")]
    Limit2Changed = 1 << 22,

    [Description("LED channel2: Adjusted current exceeds the current limit and was set to limit.")]
    Limit2 = 1 << 23,

    [Description("The bit 'Limit3' has changed.")]
    Limit3Changed = 1 << 24,

    [Description("LED channel3: Adjusted current exceeds the current limit and was set to limit.")]
    Limit3 = 1 << 25,

    [Description("The bit 'Limit4' has changed.")]
    Limit4Changed = 1 << 26,

    [Description("LED channel4: Adjusted current exceeds the current limit and was set to limit.")]
    Limit4 = 1 << 27,

    [Description("Interface data refresh occurred.")]
    InterfaceRefresh = 1 << 28
}
