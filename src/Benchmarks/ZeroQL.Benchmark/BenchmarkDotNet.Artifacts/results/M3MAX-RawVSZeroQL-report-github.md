``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 15.0.1 (24A348) [Darwin 24.0.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK=9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 9.0.0 (9.0.24.52809), Arm64 RyuJIT AdvSIMD


```
|                     Method |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
|                        Raw | 64.68 μs | 0.409 μs | 0.383 μs | 0.4883 |   5.32 KB |
|            StrawberryShake | 69.06 μs | 0.371 μs | 0.347 μs | 1.3428 |  11.79 KB |
| ZeroQLLambdaWithoutClosure | 66.61 μs | 0.457 μs | 0.428 μs | 0.8545 |   7.07 KB |
|    ZeroQLLambdaWithClosure | 66.90 μs | 0.419 μs | 0.392 μs | 0.8545 |   7.55 KB |
|              ZeroQLRequest | 66.47 μs | 0.488 μs | 0.457 μs | 0.7324 |   6.65 KB |
