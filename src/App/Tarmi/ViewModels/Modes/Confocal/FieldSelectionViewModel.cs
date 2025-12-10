using Tarmi.Confocal;
using Tarmi.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using UnitsNet;
using UnitsNet.Units;

namespace Tarmi.App.ViewModels.Modes.Confocal;

public partial class FieldSelectionViewModel : ObservableObject
{
    [ObservableProperty]
    public partial FieldSelectionArea? FieldSelectionArea { get; private set; }

    private readonly IConfocalDevice _confocalDevice;

    public FieldSelectionViewModel(IConfocalDevice confocalDevice)
    {
        _confocalDevice = confocalDevice;
    }

    internal void UpdateFieldSelectionArea(FieldSelectionArea rawFieldSelectionArea, Size imageSize)
    {
        var left = new Ratio(rawFieldSelectionArea.X / imageSize.Width, RatioUnit.DecimalFraction);
        var top = new Ratio(rawFieldSelectionArea.Y / imageSize.Height, RatioUnit.DecimalFraction);

        var right = new Ratio((rawFieldSelectionArea.X + rawFieldSelectionArea.Width) / imageSize.Width, RatioUnit.DecimalFraction);
        var bottom = new Ratio((rawFieldSelectionArea.Y + rawFieldSelectionArea.Height) / imageSize.Height, RatioUnit.DecimalFraction);

        var newRectangle = new RatioRectangle() { Left = left, Right = right, Top = top, Bottom = bottom };

        FieldSelectionArea = new FieldSelectionArea()
        {
            X = newRectangle.Left.DecimalFractions * newRectangle.Width.DecimalFractions,
            Y = newRectangle.Top.DecimalFractions * newRectangle.Height.DecimalFractions,
            Width = (newRectangle.Right - newRectangle.Left).DecimalFractions * newRectangle.Width.DecimalFractions,
            Height = (newRectangle.Bottom - newRectangle.Top).DecimalFractions * newRectangle.Height.DecimalFractions,
            //OnResizeFinished
        };
    }

    /// <summary>
    /// Add field selection area.
    /// </summary>
    /// <param name="imageSize">Image size with pixel size applied.</param>
    [RelayCommand]
    internal void AddFieldSelectionArea(Size imageSize)
    {
        var updatedImageSize = new Size()
        {
            Width = imageSize.Width > _confocalDevice.FieldWidth.Nanometers ? (int)_confocalDevice.FieldWidth.Nanometers : imageSize.Width,
            Height = imageSize.Height > _confocalDevice.FieldHeight.Nanometers ? (int)_confocalDevice.FieldHeight.Nanometers : imageSize.Height
        };

        var rawFieldSelectionArea = new FieldSelectionArea()
        {
            X = (updatedImageSize.Width - _confocalDevice.FieldWidth.Nanometers) / 2.0,
            Y = (updatedImageSize.Height - _confocalDevice.FieldHeight.Nanometers) / 2.0,
            Width = _confocalDevice.FieldWidth.Nanometers,
            Height = _confocalDevice.FieldHeight.Nanometers,
        };

        UpdateFieldSelectionArea(rawFieldSelectionArea, updatedImageSize);
    }

    [RelayCommand]
    internal void Close() => FieldSelectionArea = null;
}
