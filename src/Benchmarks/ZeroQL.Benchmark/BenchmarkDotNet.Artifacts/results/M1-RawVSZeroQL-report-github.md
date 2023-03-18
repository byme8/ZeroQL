``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 13.2.1 (22D68) [Darwin 22.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD


```
|              Method |     Mean |   Error |  StdDev |   Gen0 | Allocated |
|-------------------- |---------:|--------:|--------:|-------:|----------:|
|                 Raw | 172.2 μs | 1.49 μs | 1.40 μs | 0.7324 |   4.96 KB |
|     StrawberryShake | 175.0 μs | 1.18 μs | 1.05 μs | 1.4648 |   9.32 KB |
|        ZeroQLLambda | 174.2 μs | 1.26 μs | 1.17 μs | 0.7324 |    5.5 KB |
|       ZeroQLRequest | 174.8 μs | 1.68 μs | 1.49 μs | 0.7324 |   5.88 KB |
|  ZeroQLLambdaUpload | 208.5 μs | 2.06 μs | 1.83 μs | 1.4648 |  10.34 KB |
| ZeroQLRequestUpload | 208.9 μs | 3.02 μs | 2.83 μs | 1.7090 |  10.43 KB |
