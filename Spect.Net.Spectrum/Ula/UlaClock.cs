﻿using System;
using System.Threading;
using Spect.Net.Spectrum.Utilities;
// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace Spect.Net.Spectrum.Ula
{
    /// <summary>
    /// This class represents the ULA clock that drives both the ULA chip and Z80
    /// in the Spectrum 48K model
    /// </summary>
    public class UlaClock: IHighResolutionClockProvider
    {
        private double _z80ClockTick;
        private double _z80NopTick;
        private readonly IHighResolutionClockProvider _clockProvider;
        private long _frequency;
        private long _ticksAtStart;

        /// <summary>
        /// The CPU uses 3.5 MHz clock
        /// </summary>
        public const double CPU_FREQ_MHZ = 3.5;

        /// <summary>
        /// The number of performance counter ticks for a single Z80 clock
        /// signal
        /// </summary>
        public double Z80ClockTick => _z80ClockTick;

        /// <summary>
        /// The number of performance counter ticks for a single Z80 NOP
        /// instruction
        /// </summary>
        public double Z80NopTick => _z80NopTick;

        /// <summary>
        /// Retrieves the frequency of the performance counter that measures
        /// the system time with less than 1us prcision.
        /// </summary>
        public long Frequency => _frequency;

        /// <summary>
        /// Gets the initial performance counter value when the ULA clock
        /// has been reset last time
        /// </summary>
        public long TicksAtStart => _ticksAtStart;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public UlaClock(IHighResolutionClockProvider clockProvider = null)
        {
            _clockProvider = clockProvider ?? this;
            Reset();
        }

        /// <summary>
        /// Resets the clock (just as if the ULA chip was reset)
        /// </summary>
        public void Reset()
        {
            _frequency = _clockProvider.GetFrequency();
            _z80ClockTick = _frequency/CPU_FREQ_MHZ/1000000;
            _z80NopTick = 4*_z80ClockTick;
            _ticksAtStart = _clockProvider.GetCounter();
        }

        /// <summary>
        /// Gets the native high resolution counter's value
        /// </summary>
        /// <returns>Native counter value</returns>
        public long GetNativeCounter()
        {
            return _clockProvider.GetCounter();
        }

        /// <summary>
        /// Gets the number of ticks from the last reset
        /// </summary>
        public long Z80TickCount
        {
            get
            {
                var clockTicks = _clockProvider.GetCounter();
                return (long)((clockTicks - _ticksAtStart)/_z80ClockTick);
            }
        }

        /// <summary>
        /// Wait until the specified counter value is reached
        /// </summary>
        /// <param name="counterValue">Counter value to reach</param>
        /// <param name="token">Token that can cancel the wait cycle</param>
        public void WaitUntil(long counterValue, CancellationToken token)
        {
            // --- Calculate the number of milliseconds to wait
            var millisec = _frequency / 1000;

            // --- Wait until we have up to 4 milliseconds left
            while (!token.IsCancellationRequested)
            {
                var millisecs = (counterValue - _clockProvider.GetCounter()) / millisec;
                if (millisecs < 0)
                {
                    return;
                }
                if (millisecs < 4) break;
                Thread.Sleep(2);
            }

            // --- Use SpinWait
            while (!token.IsCancellationRequested)
            {
                if (counterValue < _clockProvider.GetCounter()) break;
                Thread.SpinWait(1);
            }
        }

        #region Implementation of IHighResolutionClockProvider

        /// <summary>
        /// Retrieves the frequency of the clock. This value shows new
        /// number of clock ticks per second.
        /// </summary>
        long IHighResolutionClockProvider.GetFrequency()
        {
            long frequency;
            if (!KernelMethods.QueryPerformanceFrequency(out frequency))
            {
                throw new InvalidOperationException("Performance frequecy cannot be queried.");
            }
            return frequency;
        }

        /// <summary>
        /// Retrieves the current counter value of the clock.
        /// </summary>
        long IHighResolutionClockProvider.GetCounter()
        {
            long perfValue;
            KernelMethods.QueryPerformanceCounter(out perfValue);
            return perfValue;
        }

        #endregion
    }
}