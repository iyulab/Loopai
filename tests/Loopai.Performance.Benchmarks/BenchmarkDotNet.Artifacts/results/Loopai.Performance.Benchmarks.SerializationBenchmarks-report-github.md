```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26200.6899)
13th Gen Intel Core i7-1360P 2.20GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| GetRawTextSimple       |  35.79 ns | 211.15 ns | 11.574 ns |  0.25 |    0.07 |    1 | 0.0076 |      72 B |        0.33 |
| GetRawTextComplex      |  54.77 ns |  38.29 ns |  2.099 ns |  0.39 |    0.01 |    2 | 0.0714 |     672 B |        3.11 |
| SerializeSimpleObject  | 140.87 ns | 618.84 ns | 33.921 ns |  1.00 |    0.21 |    3 | 0.0076 |      72 B |        0.33 |
| ParseSimpleJson        | 141.39 ns |  26.99 ns |  1.480 ns |  1.00 |    0.01 |    3 | 0.0229 |     216 B |        1.00 |
| SerializeComplexObject | 589.38 ns | 185.21 ns | 10.152 ns |  4.17 |    0.07 |    4 | 0.0744 |     704 B |        3.26 |
| ParseComplexJson       | 787.25 ns |  48.56 ns |  2.662 ns |  5.57 |    0.05 |    5 | 0.1211 |    1144 B |        5.30 |
