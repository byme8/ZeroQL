``` ini

BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.5.1 (21G83) [Darwin 21.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.400
  [Host]     : .NET 6.0.8 (6.0.822.36306), Arm64 RyuJIT
  DefaultJob : .NET 6.0.8 (6.0.822.36306), Arm64 RyuJIT


```
|              Method |     Mean |   Error |  StdDev |  Gen 0 | Allocated |
|-------------------- |---------:|--------:|--------:|-------:|----------:|
|                 Raw | 184.9 μs | 1.00 μs | 0.94 μs | 2.4414 |      5 KB |
|     StrawberryShake | 193.2 μs | 0.76 μs | 0.68 μs | 3.1738 |      6 KB |
|        ZeroQLLambda | 188.1 μs | 1.64 μs | 1.54 μs | 2.6855 |      6 KB |
|       ZeroQLRequest | 188.1 μs | 0.92 μs | 0.86 μs | 2.9297 |      6 KB |
|  ZeroQLLambdaUpload | 231.7 μs | 1.28 μs | 1.20 μs | 5.3711 |     11 KB |
| ZeroQLRequestUpload | 232.7 μs | 1.69 μs | 1.58 μs | 5.3711 |     11 KB |
