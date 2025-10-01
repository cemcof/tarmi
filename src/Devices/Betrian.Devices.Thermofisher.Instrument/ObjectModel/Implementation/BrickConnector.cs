using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Fei.XT.Common.gen;
using Fei.XT.Instrument.gen;
using Fei.XT.Server.BrickConnector;
using Fei.XT.Server.BrickConnector.Exceptions;
using Fei.XT.ViewServer.gen;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel.Implementation;

internal class BrickConnector : IBrickConnector
{
    private static readonly Guid FeiObjectModelClsid = new("0DAA81A9-5956-48B8-9F6C-75727BFBDB91");
    private static readonly Type? FeiObjectModelType = Marshal.GetTypeFromCLSID(FeiObjectModelClsid);
    private static readonly Guid ViewServerClsid = new("17209DC1-F4AA-4EE9-A98F-F03E09374C87");
    private static readonly Type? ViewServerType = Marshal.GetTypeFromCLSID(ViewServerClsid);

    private static readonly ImmutableDictionary<string, ImmutableList<string>> UserNamePasswords = new Dictionary<string, ImmutableList<string>>
    {
        ["factory"] = ["FGPR8CNR7iU=", "mG3YJ5DYFo58T2oHwmyfuw==", "93h/Tj2tze6WKjUyOLvnKA=="],
        ["service"] = ["jzrV8Le9gzQ="],
        ["support"] = ["MI4u+gpZqZQEJGjtC5Qyiw==", "/B17XVz/xHLu1Km6BaNfHQ=="],
        ["supervisor"] = ["GTWofD3W/zot1LOKECro+A==", "6ffa6NOUR2c=", "owEEHCvAnkQt1LOKECro+A=="],
        ["user"] = ["g3YGvzmOYpw=", "jPukrTp2BPI=", "8R8nyosy8bg="]
    }.ToImmutableDictionary();

    private const string DefaultUserName = "user";

    private IFeiObjectmodelObject2? _objectModel;
    private ViewServer? _viewServer;
    private Session? _session;
    private readonly object _comLock = new();
    private readonly BehaviorSubject<bool> _isConnectedSubject = new(false);

    public BrickConnector()
    {
    }

    public IObservable<bool> IsConnected
        => _isConnectedSubject.AsObservable().DistinctUntilChanged();

    private void EnsureObjectModel()
    {
        lock (_comLock)
        {
            try
            {
                _objectModel ??= (FeiObjectmodelObject?)Activator.CreateInstance(FeiObjectModelType!) as IFeiObjectmodelObject2;
                _viewServer ??= (ViewServer?)Activator.CreateInstance(ViewServerType!);
                _session ??= (Session)_objectModel!.GetObject(PathLiterals.Instrument.Service.Session.AsString);
                _isConnectedSubject.OnNext(true);
            }
            catch
            {
                _session = null;
                _viewServer = null;
                _objectModel = null;
            }
        }
    }

    public void DisconnectionDetected()
    {
        lock (_comLock)
        {
            _session = null;
            _viewServer = null;
            _objectModel = null;
            _isConnectedSubject.OnNext(false);
        }
    }

    public Result<T> GetObject<T>(string objectPath)
        where T : class
    {
        try
        {
            return new Result<T>(GetObjectImpl<T>(objectPath));
        }
        catch (Exception ex)
        {
            if (ex is COMException cex)
            {
                if (cex.ErrorCode == ErrorCodes.CannotConnectToMicroscope)
                {
                    DisconnectionDetected();
                    var exception = new InvalidOperationException(ErrorCodes.XtServerNotRunningMessage, cex);
                    return new Result<T>(exception);
                }
                return cex.MapToResult<T>();
            }
            return ex.MapToResult<T>();
        }
    }

    private T GetObjectImpl<T>(string path)
        where T : class
    {
        try
        {
            EnsureObjectModel();
            PrepareGetObject(path);
            return _objectModel?.GetObject(path) is not T obj
                ? throw new NoInterfaceException($"The object '{path}' does not implement interface {typeof(T)}")
                : obj;
        }
        catch (COMException ex)
        {
            throw new ConnectionFailedException($"Couldn't connect to '{path}'\nThe path probably does not exist.\n\n{ex.Message}");
        }
    }

    public Result<PatternDataSource> GetPatterningDataSource(string viewName)
    {
        try
        {
            EnsureObjectModel();
            PrepareGetObject(PathLiterals.Instrument.Options.AsString); // dummy path for initialization
            return new(GetPatterningDataSourceImpl(viewName));
        }
        catch (COMException ex)
        {
            if (ex is COMException cex)
            {
                if (cex.ErrorCode == ErrorCodes.CannotConnectToMicroscope)
                {
                    DisconnectionDetected();
                    var exception = new InvalidOperationException(ErrorCodes.XtServerNotRunningMessage, cex);
                    return new Result<PatternDataSource>(exception);
                }
            }
            return ex.MapToResult<PatternDataSource>();
        }
    }

    public Result<ViewServer> GetViewServer()
    {
        try
        {
            EnsureObjectModel();
            PrepareGetObject(PathLiterals.Instrument.Options.AsString); // dummy path for initialization
            return new Result<ViewServer>(_viewServer!);
        }
        catch (COMException ex)
        {
            if (ex is COMException cex)
            {
                if (cex.ErrorCode == ErrorCodes.CannotConnectToMicroscope)
                {
                    DisconnectionDetected();
                    var exception = new InvalidOperationException(ErrorCodes.XtServerNotRunningMessage, cex);
                    return new Result<ViewServer>(exception);
                }
            }
            return ex.MapToResult<ViewServer>();
        }
    }

    public Result<View> GetView(string viewName)
    {
        try
        {
            EnsureObjectModel();
            PrepareGetObject(PathLiterals.Instrument.Options.AsString); // dummy path for initialization
            return new(GetViewImpl(viewName));
        }
        catch (COMException ex)
        {
            if (ex is COMException cex)
            {
                if (cex.ErrorCode == ErrorCodes.CannotConnectToMicroscope)
                {
                    DisconnectionDetected();
                    var exception = new InvalidOperationException(ErrorCodes.XtServerNotRunningMessage, cex);
                    return new Result<View>(exception);
                }
            }
            return ex.MapToResult<View>();
        }
    }

    public View GetViewImpl(string viewName)
    {
        try
        {
            EnsureObjectModel();
            return _viewServer!.Views[viewName];
        }
        catch
        {
            throw new ConnectionFailedException("Couldn't connect to view server");
        }
    }

    public PatternDataSource GetPatterningDataSourceImpl(string viewName)
    {
        try
        {
            EnsureObjectModel();
            return (PatternDataSource)_viewServer!.Views[viewName].Datasources["Patterning"];
        }
        catch
        {
            throw new ConnectionFailedException("Couldn't connect to view server");
        }
    }

    private bool Login(string user)
    {

        foreach (var userNamePassword in GetUserNamePasswords(user))
        {
            try
            {
                _session!.Login(user, userNamePassword);
                return true;
            }
            catch (Exception)
            {
            }
        }

        return false;
    }

    private bool LoginAsDefaultUserIfNotLoggedInAlready()
    {
        try
        {
            lock (_comLock)
            {
                return IsLoggedIn() || Login(DefaultUserName);
            }
        }
        catch (Exception)
        {
        }

        return false;
    }

    private bool IsLoggedIn()
    {
        var result = _session!.LoggedIn;
        return result;
    }

    private void PrepareGetObject(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path to GetObject can't be empty!");
        }

        if (!LoginAsDefaultUserIfNotLoggedInAlready())
        {
            throw new LoginFailedException("Could not login to ObjectModel, server is probably not running");
        }
    }

    private static IEnumerable<string> GetUserNamePasswords(string userName)
    {
        foreach (var item in UserNamePasswords[userName.ToLower()])
        {
            yield return Decrypt(item);
        }
    }

    private static string Decrypt(string input)
    {
        var s = "This program \u0001cannot be ";
        var array = Convert.FromBase64String(input);
        using var tripleDESCryptoServiceProvider = TripleDES.Create();
        tripleDESCryptoServiceProvider.Key = Encoding.UTF8.GetBytes(s);
        tripleDESCryptoServiceProvider.Mode = CipherMode.ECB;
        tripleDESCryptoServiceProvider.Padding = PaddingMode.PKCS7;
        using var cryptoTransform = tripleDESCryptoServiceProvider.CreateDecryptor();
        var bytes = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
        tripleDESCryptoServiceProvider.Clear();
        return Encoding.UTF8.GetString(bytes);
    }
}
