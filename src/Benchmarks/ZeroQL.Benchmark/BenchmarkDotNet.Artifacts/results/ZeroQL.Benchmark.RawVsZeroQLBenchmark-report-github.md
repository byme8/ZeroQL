``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 14.0 (23A344) [Darwin 23.0.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.400
  [Host]     : .NET 7.0.10 (7.0.1023.36312), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.10 (7.0.1023.36312), Arm64 RyuJIT AdvSIMD


```
|                  Method |      Mean |    Error |   StdDev |   Gen0 |   Gen1 | Allocated |
|------------------------ |----------:|---------:|---------:|-------:|-------:|----------:|
|                    Noop |  80.39 μs | 1.455 μs | 1.290 μs | 0.4883 |      - |   3.05 KB |
|                     Raw | 217.66 μs | 1.219 μs | 1.141 μs | 8.3008 | 0.4883 |  50.56 KB |
|          RawMessagePack | 210.67 μs | 0.722 μs | 0.640 μs | 3.4180 |      - |  19.89 KB |
| ZeroQLLambdaWithClosure | 216.32 μs | 2.182 μs | 1.935 μs | 3.9063 |      - |  23.95 KB |
