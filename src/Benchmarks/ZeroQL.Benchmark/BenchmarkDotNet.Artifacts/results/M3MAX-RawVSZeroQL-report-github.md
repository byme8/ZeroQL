```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                     | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
| Raw                        | 62.29 μs | 0.504 μs | 0.421 μs | 0.4883 |   5.13 KB |
| StrawberryShake            | 65.10 μs | 0.362 μs | 0.302 μs | 1.3428 |  11.22 KB |
| ZeroQLLambdaWithoutClosure | 63.91 μs | 1.203 μs | 1.181 μs | 0.8545 |    6.9 KB |
| ZeroQLLambdaWithClosure    | 65.59 μs | 1.261 μs | 1.595 μs | 0.8545 |   7.38 KB |
| ZeroQLRequest              | 64.49 μs | 1.244 μs | 1.163 μs | 0.7324 |   6.48 KB |
