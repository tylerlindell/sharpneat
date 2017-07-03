/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software; you can redistribute it and/or modify
 * it under the terms of The MIT License (MIT).
 *
 * You should have received a copy of the MIT License
 * along with SharpNEAT; if not, see https://opensource.org/licenses/MIT.
 */

using System;
using System.Runtime.CompilerServices;

namespace SharpNeat.Network
{
    /// <summary>
    /// The logistic function implemented using a fast to compute approximation of exp().
    /// See:
    ///   https://stackoverflow.com/a/412988/15703
    ///   https://pdfs.semanticscholar.org/35d3/2b272879a2018a2d33d982639d4be489f789.pdf (A Fast, Compact Approximation of the Exponential Function)
    /// </summary>
    public class LogisticApproximantSteep : IActivationFunction
    {
        public string Id => "LogisticApproximantSteep";

        public double Fn(double x)
        {
            return 1.0/(1.0 + ExpApprox(-4.9*x));
        }

        // Fast exp approximation, from:
        // https://stackoverflow.com/a/412988/15703
        // https://pdfs.semanticscholar.org/35d3/2b272879a2018a2d33d982639d4be489f789.pdf (A Fast, Compact Approximation of the Exponential Function)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ExpApprox(double val)
        {
            long tmp = (long)(1512775 * val + (1072693248 - 60801));
            return BitConverter.Int64BitsToDouble(tmp << 32);
        }
    }
}
