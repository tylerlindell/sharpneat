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
using SharpNeat.Network;

// Disable missing comment warnings for non-private variables.
#pragma warning disable 1591

namespace SharpNeat.Phenomes.NeuralNets
{
    /// <summary>
    /// A neural network class that represents a network with recurrent (cyclic) connections. 
    /// 
    /// This class contains performance improvements described in the following report/post:
    /// 
    ///     http://sharpneat.sourceforge.net/research/network-optimisations.html
    /// 
    /// A speedup over a previous 'naive' implementation was achieved by compactly storing all required data in arrays
    /// and in a way that maximizes in-order memory accesses; this allows for good utilisation of CPU caches. 
    /// 
    /// Algorithm Overview.
    /// 1) Loop connections. Each connection gets its input signal from its source neuron, applies its weight and
    /// stores its output value./ Connections are ordered by source neuron index, thus all memory accesses here are
    /// sequential/in-order.
    /// 
    /// 2) Loop connections (again). Each connection adds its output value to its target neuron, thus each neuron  
    /// accumulates or 'collects' its input signal in its pre-activation variable. Because connections are sorted by
    /// source neuron index and not target index, this loop generates out-of order memory accesses, but is the only 
    /// loop to do so.
    /// 
    /// 3) Loop neurons. Pass each neuron's pre-activation signal through the activation function and set its 
    /// post-activation signal value. 
    /// 
    /// The activation loop is now complete and we can go back to (1) or stop.
    /// </summary>
    public class HeterogeneousCyclicNetwork : IBlackBox
    {
        protected readonly ConnectionInfo[] _connectionArray;
        protected readonly Func<double,double>[] _activationFnArr;

        // Neuron pre- and post-activation signal arrays.
        protected readonly double[] _preActivationArray;
        protected readonly double[] _postActivationArray;

        // Wrappers over _postActivationArray that map between black box inputs/outputs to the
        // corresponding underlying network state variables.
        readonly SignalArray _inputSignalArrayWrapper;
        readonly SignalArray _outputSignalArrayWrapper;

        // Convenient counts.
        readonly int _inputNeuronCount;
        readonly int _outputNeuronCount;
        protected readonly int _inputAndBiasNeuronCount;
        protected readonly int _timestepsPerActivation;

        #region Constructor

        /// <summary>
        /// Constructs a CyclicNetwork with the provided pre-built ConnectionInfo array and 
        /// associated data.
        /// </summary>
        public HeterogeneousCyclicNetwork(ConnectionInfo[] connInfoArr,
                             Func<double,double>[] neuronActivationFnArray,
                             int neuronCount,
                             int inputNeuronCount,
                             int outputNeuronCount,
                             int timestepsPerActivation,
                             bool boundedOutput)
        {
            _connectionArray = connInfoArr;
            _activationFnArr = neuronActivationFnArray;

            // Create neuron pre- and post-activation signal arrays.
            _preActivationArray = new double[neuronCount];
            _postActivationArray = new double[neuronCount];

            // Wrap sub-ranges of the neuron signal arrays as input and output arrays for IBlackBox.
            // Offset is 1 to skip bias neuron (The value at index 1 is the first black box input).
            _inputSignalArrayWrapper = new SignalArray(_postActivationArray, 1, inputNeuronCount);

            // Offset to skip bias and input neurons. Output neurons follow input neurons in the arrays.
            if(boundedOutput) {
                _outputSignalArrayWrapper = new OutputSignalArray(_postActivationArray, inputNeuronCount+1, outputNeuronCount);
            } else {
                _outputSignalArrayWrapper = new SignalArray(_postActivationArray, inputNeuronCount + 1, outputNeuronCount);
            }

            // Store counts for use during activation.
            _inputNeuronCount = inputNeuronCount;
            _inputAndBiasNeuronCount = inputNeuronCount+1;
            _outputNeuronCount = outputNeuronCount;
            _timestepsPerActivation = timestepsPerActivation;

            // Initialise the bias neuron's fixed output value.
            _postActivationArray[0] = 1.0;
        }

        #endregion

        #region IBlackBox Members

        /// <summary>
        /// Gets the number of inputs.
        /// </summary>
        public int InputCount
        {
            get { return _inputNeuronCount; }
        }

        /// <summary>
        /// Gets the number of outputs.
        /// </summary>
        public int OutputCount
        {
            get { return _outputNeuronCount; }
        }

        /// <summary>
        /// Gets an array for feeding input signals to the network.
        /// </summary>
        public ISignalArray InputSignalArray
        {
            get { return _inputSignalArrayWrapper; }
        }

        /// <summary>
        /// Gets an array of output signals from the network.
        /// </summary>
        public ISignalArray OutputSignalArray
        {
            get { return _outputSignalArrayWrapper; }
        }

        /// <summary>
        /// Activate the network for a fixed number of iterations defined by the 'maxIterations' parameter
        /// at construction time. Activation reads input signals from InputSignalArray and writes output signals
        /// to OutputSignalArray.
        /// </summary>
        public virtual void Activate()
        {
            // Activate the network for a fixed number of timesteps.
            for(int i=0; i<_timestepsPerActivation; i++)
            {
                // Loop connections. Get each connection's input signal, apply the weight and add the result to 
                // the pre-activation signal of the target neuron.
                for(int j=0; j<_connectionArray.Length; j++) {
                    _preActivationArray[_connectionArray[j]._tgtNeuronIdx] += _postActivationArray[_connectionArray[j]._srcNeuronIdx] * _connectionArray[j]._weight;
                }

                // TODO: Performance tune the activation function method call.
                // The call to Calculate() cannot be inlined because it is via an interface and therefore requires a virtual table lookup.
                // The obvious/simplest performance improvement would be to pass an array of values to Calculate().

                // Loop the neurons. Pass each neuron's pre-activation signals through its activation function
                // and store the resulting post-activation signal.
                // Skip over bias and input neurons as these have no incoming connections and therefore have fixed
                // post-activation values and are never activated. 
                for (int j=_inputAndBiasNeuronCount; j<_preActivationArray.Length; j++)
                {
                    _postActivationArray[j] = _activationFnArr[j](_preActivationArray[j]);
                    
                    // Take the opportunity to reset the pre-activation signal array in preparation for the next 
                    // activation loop.
                    _preActivationArray[j] = 0.0;
                }
            }
        }

        /// <summary>
        /// Reset the network's internal state.
        /// </summary>
        public void ResetState()
        {
            // TODO: Avoid resetting if network state hasn't changed since construction or previous reset.

            // Reset the output signal for all output and hidden neurons.
            // Ignore connection signal state as this gets overwritten on each iteration.
            for(int i=_inputAndBiasNeuronCount; i<_postActivationArray.Length; i++) {
                _preActivationArray[i] = 0.0;
                _postActivationArray[i] = 0.0;
            }
        }

        #endregion
    }
}