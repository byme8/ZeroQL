cd src/LinqQL
dotnet build -c Release
dotnet pack -c Release -o ../../nugets

cd ../LinqQL.CLI
dotnet build -c Release
dotnet pack -c Release -o ../../nugets

cd ../..