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

namespace SharpNeat.Network
{
    /// <summary>
    /// The softsign sigmoid.
    /// This is a variant of softsign that has a steeper slope at and around the origin that 
    /// is intended to be a similar slope to that of LogisticFunctionSteep.
    /// </summary>
    public class SoftSignSteep : IActivationFunction
    {
        public string Id => "ScaledELU";

        /// <summary>
        /// Calculates the output value for the specified input value.
        /// </summary>
        public double Fn(double x)
        {
            return 0.5 + (x / (2.0 * ( 0.2 + Math.Abs(x))));
        }
    }
}
