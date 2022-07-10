``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.4 (21F79) [Darwin 21.5.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.301
  [Host]     : .NET 6.0.6 (6.0.622.26707), Arm64 RyuJIT
  DefaultJob : .NET 6.0.6 (6.0.622.26707), Arm64 RyuJIT


```
|          Method |     Mean |   Error |  StdDev |  Gen 0 | Allocated |
|---------------- |---------:|--------:|--------:|-------:|----------:|
|             Raw | 182.9 μs | 1.43 μs | 1.33 μs | 2.4414 |      5 KB |
| StrawberryShake | 190.3 μs | 1.09 μs | 1.02 μs | 3.1738 |      6 KB |
|          ZeroQL | 184.8 μs | 0.94 μs | 0.83 μs | 2.9297 |      6 KB |
