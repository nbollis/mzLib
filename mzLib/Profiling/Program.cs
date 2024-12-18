// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Profiling;

var summary = BenchmarkRunner.Run<ToleranceBenchmarks>();
