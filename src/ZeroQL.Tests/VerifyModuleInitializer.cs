using System.Runtime.CompilerServices;

namespace ZeroQL.Tests;

public static class VerifyModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySystemJson.Initialize();
        VerifierSettings.IgnoreMember(nameof(GraphQLResult<Unit>.HttpResponseMessage));
    }
}