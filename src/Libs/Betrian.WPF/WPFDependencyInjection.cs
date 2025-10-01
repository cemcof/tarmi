namespace Betrian.WPF;

public static class WPFDependencyInjection
{
    public static IServiceProvider ServiceProvider { get; set; } = new EmptyServiceProvider();

    private class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => throw new InvalidOperationException("Attempted to retrieve a service before initializing IServiceProvider.");
    }

}
