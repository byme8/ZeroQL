``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 14.0 (23A344) [Darwin 23.0.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.400
  [Host]     : .NET 7.0.10 (7.0.1023.36312), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.10 (7.0.1023.36312), Arm64 RyuJIT AdvSIMD


```
|                  Method |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------------------------ |----------:|---------:|---------:|-------:|----------:|
|                    Noop |  84.45 μs | 0.685 μs | 0.572 μs | 0.4883 |   3.05 KB |
|                     Raw | 128.60 μs | 2.370 μs | 2.217 μs | 0.9766 |   5.98 KB |
| ZeroQLLambdaWithClosure | 129.64 μs | 1.599 μs | 1.496 μs | 0.9766 |   7.03 KB |
