param (
    [string]$version = (Get-Date -Format "999.yyMM.ddHH.mmss")
) 

dotnet clean
dotnet build -c Release /p:Version=$version
dotnet pack -c Release ./src/ZeroQL.Runtime/ZeroQL.Runtime.csproj --no-build --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.Tools/ZeroQL.Tools.csproj --no-build --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.Package/ZeroQL.Package.csproj --no-build --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroQL.CLI/ZeroQL.CLI.csproj --no-build --verbosity normal /p:Version=$version -o ./nugets
