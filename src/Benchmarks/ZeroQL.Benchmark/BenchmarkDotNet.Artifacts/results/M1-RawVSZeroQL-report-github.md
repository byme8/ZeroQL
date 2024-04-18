``` ini

BenchmarkDotNet=v0.13.2, OS=macOS 14.3.1 (23D60) [Darwin 23.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.0 (8.0.23.53103), Arm64 RyuJIT AdvSIMD


```
| Method                     |     Mean |   Error |  StdDev |   Gen0 | Allocated |
|----------------------------|---------:|--------:|--------:|-------:|----------:|
| Raw                        | 111.3 us | 0.77 us | 0.68 us | 0.7324 |   5.29 KB |
| StrawberryShake            | 119.3 us | 1.61 us | 1.51 us | 1.7090 |  11.55 KB |
| ZeroQLLambdaWithoutClosure | 112.4 us | 2.04 us | 1.91 us | 0.9766 |    6.7 KB |
| ZeroQLLambdaWithClosure    | 113.7 us | 1.80 us | 1.68 us | 0.9766 |   7.18 KB |
| ZeroQLRequest              | 112.9 us | 1.22 us | 1.14 us | 0.9766 |   6.27 KB |