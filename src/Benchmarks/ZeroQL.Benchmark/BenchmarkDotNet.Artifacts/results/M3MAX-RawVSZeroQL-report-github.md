``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 14.5 (23F79) [Darwin 23.5.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK=8.0.301
  [Host]     : .NET 8.0.6 (8.0.624.26715), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.6 (8.0.624.26715), Arm64 RyuJIT AdvSIMD


```
|                     Method |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
|                        Raw | 68.65 μs | 0.277 μs | 0.231 μs | 0.6104 |   5.34 KB |
|            StrawberryShake | 73.48 μs | 0.362 μs | 0.321 μs | 1.3428 |  11.58 KB |
| ZeroQLLambdaWithoutClosure | 69.55 μs | 0.376 μs | 0.351 μs | 0.7324 |   6.74 KB |
|    ZeroQLLambdaWithClosure | 70.43 μs | 0.439 μs | 0.410 μs | 0.8545 |   7.22 KB |
|              ZeroQLRequest | 69.95 μs | 0.439 μs | 0.366 μs | 0.7324 |   6.32 KB |
