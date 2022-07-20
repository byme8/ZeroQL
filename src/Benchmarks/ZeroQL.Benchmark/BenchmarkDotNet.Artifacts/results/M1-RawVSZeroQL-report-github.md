``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.4 (21F79) [Darwin 21.5.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.302
  [Host]     : .NET 6.0.7 (6.0.722.32202), Arm64 RyuJIT
  DefaultJob : .NET 6.0.7 (6.0.722.32202), Arm64 RyuJIT


```
|          Method |     Mean |   Error |  StdDev |  Gen 0 | Allocated |
|---------------- |---------:|--------:|--------:|-------:|----------:|
|             Raw | 182.5 μs | 1.07 μs | 1.00 μs | 2.4414 |      5 KB |
| StrawberryShake | 190.9 μs | 0.74 μs | 0.69 μs | 3.1738 |      6 KB |
|          ZeroQL | 185.9 μs | 1.39 μs | 1.30 μs | 2.9297 |      6 KB |
