```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26200.6899)
13th Gen Intel Core i7-1360P 2.20GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                             | Mean           | Error          | StdDev        | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------- |---------------:|---------------:|--------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| WriteToMemoryStream                |       880.2 ns |     1,540.4 ns |      84.43 ns | 0.002 |    0.00 |    1 | 1.0414 | 0.0381 |    9800 B |        8.39 |
| WriteAndDeleteTempFileSync         |   343,618.7 ns |   127,877.1 ns |   7,009.38 ns | 0.881 |    0.02 |    2 |      - |      - |     464 B |        0.40 |
| WriteAndDeleteTempFile             |   390,007.4 ns |   167,509.9 ns |   9,181.78 ns | 1.000 |    0.03 |    2 |      - |      - |    1168 B |        1.00 |
| WriteAndDeleteTempFileWithEncoding |   444,657.2 ns |   230,818.5 ns |  12,651.94 ns | 1.141 |    0.04 |    2 |      - |      - |    1200 B |        1.03 |
| MultipleFileOperations             | 2,585,347.1 ns | 2,328,666.0 ns | 127,642.04 ns | 6.631 |    0.31 |    3 |      - |      - |    6097 B |        5.22 |
