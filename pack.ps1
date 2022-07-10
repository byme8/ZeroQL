cd src/ZeroQL
dotnet build -c Release
dotnet pack -c Release -o ../../nugets

cd ../ZeroQL.CLI
dotnet build -c Release
dotnet pack -c Release -o ../../nugets

cd ../..