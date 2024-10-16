﻿// <copyright file="Program.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

// ReSharper disable RedundantUsingDirective
// ReSharper disable UnusedVariable
#pragma warning disable SA1200
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using KeyboardPerf;

#if DEBUG

var keyboardStateBenchMarks = new KeyboardBenchmarks();
keyboardStateBenchMarks.GetState();

#elif RELEASE

// This is to prevent the requirement of having a release build of Carbonate
var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);

var keyboardResults = BenchmarkRunner.Run<KeyboardBenchmarks>(config);
Console.WriteLine(keyboardResults);

#endif

Console.ReadLine();
