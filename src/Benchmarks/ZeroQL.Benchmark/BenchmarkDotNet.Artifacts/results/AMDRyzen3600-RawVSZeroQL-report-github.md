``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1766 (21H1/May2021Update)
AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.301
  [Host]     : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT
  DefaultJob : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT


```
|          Method |     Mean |   Error |  StdDev |  Gen 0 | Allocated |
|---------------- |---------:|--------:|--------:|-------:|----------:|
|             Raw | 129.3 μs | 2.52 μs | 4.13 μs | 0.4883 |      5 KB |
| StrawberryShake | 133.4 μs | 0.58 μs | 0.51 μs | 0.7324 |      6 KB |
|          ZeroQL | 130.4 μs | 2.05 μs | 1.92 μs | 0.4883 |      6 KB |
