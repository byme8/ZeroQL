param (
    [string]$version = (Get-Date -Format "999.yyMM.ddHH.mmss")
) 

dotnet clean
dotnet pack -c Release ./src/ZeroQL.Runtime/ZeroQL.Runtime.csproj --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.Tools/ZeroQL.Tools.csproj --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.Package/ZeroQL.Package.csproj --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.CLI/ZeroQL.CLI.csproj --verbosity normal /p:Version=$version -o ./nugets
