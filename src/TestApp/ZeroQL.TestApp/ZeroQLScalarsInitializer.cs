using System.Runtime.CompilerServices;
using ZeroQL.Json;

namespace ZeroQL.TestApp;

public static class ZeroQLScalarsInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        ZeroQLJsonOptions.Configure(o => o.Converters
            .Add(new InstantJsonConverter()));
    }
}