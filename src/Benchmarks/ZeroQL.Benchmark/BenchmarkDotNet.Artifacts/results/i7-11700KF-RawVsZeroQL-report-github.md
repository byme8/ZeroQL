``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.2728)
11th Gen Intel Core i7-11700KF 3.60GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2


```
|              Method |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|----------:|
|                 Raw |  96.84 μs | 1.391 μs | 1.302 μs | 0.4883 |   4.45 KB |
|     StrawberryShake |  99.02 μs | 0.898 μs | 0.840 μs | 1.0986 |   8.84 KB |
|        ZeroQLLambda | 100.03 μs | 0.803 μs | 0.752 μs | 0.6104 |   4.98 KB |
|       ZeroQLRequest | 102.82 μs | 1.056 μs | 0.988 μs | 0.4883 |   5.38 KB |
|  ZeroQLLambdaUpload | 154.36 μs | 1.476 μs | 1.381 μs | 0.9766 |   9.81 KB |
| ZeroQLRequestUpload | 154.57 μs | 1.834 μs | 1.626 μs | 0.9766 |    9.9 KB |
