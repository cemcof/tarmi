namespace Tarmi.App.Controls;

public interface IScaleAwareItem
{
    void Move(double dX, double dY);

    void Resize(double dX, double dY);

    void Scale(double scale);
}
