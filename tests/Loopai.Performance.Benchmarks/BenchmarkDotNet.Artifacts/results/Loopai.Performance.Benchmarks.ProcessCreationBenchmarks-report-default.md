
BenchmarkDotNet v0.15.4, Windows 11 (10.0.26200.6899)
13th Gen Intel Core i7-1360P 2.20GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                            | Mean             | Error            | StdDev          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------------- |-----------------:|-----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
 DenoProcessCreation_SimpleEcho    | 114,950,188.9 ns | 108,787,623.6 ns |  5,963,016.8 ns | 1.002 |    0.06 |      - |   34573 B |        1.00 |
 DenoProcessCreation_JsonTransform |  87,278,205.6 ns |  51,736,891.9 ns |  2,835,873.6 ns | 0.761 |    0.04 |      - |   34821 B |        1.01 |
 DenoProcessCreation_WithTempFile  | 130,733,726.7 ns | 445,127,479.6 ns | 24,398,939.3 ns | 1.139 |    0.19 |      - |   36307 B |        1.05 |
 NoProcessCreation_DirectExecution |         639.5 ns |       2,886.9 ns |        158.2 ns | 0.000 |    0.00 | 0.0505 |     480 B |        0.01 |
