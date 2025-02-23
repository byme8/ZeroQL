using System.Runtime.CompilerServices;

namespace ZeroQL.Tests.CLI;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyNodaTime.Initialize();
    }
}