
$version = Get-Date -Format "999.yy.MM-dd-HH-mm-ss"
dotnet clean
dotnet build -c Release /p:Version=$version
dotnet pack -c Release --no-build --verbosity normal /p:Version=$version -o ./nugets