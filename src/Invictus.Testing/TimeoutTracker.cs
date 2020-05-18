using System;
using System.Collections.Generic;
using System.Text;

namespace Invictus.Testing
{
    public struct TimeoutTracker
    {
        private int _total;
        private int _start;

        public TimeoutTracker(TimeSpan timeout)
        {
            long ltm = (long)timeout.TotalMilliseconds;
            if (ltm < -1 || ltm > (long)int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            _total = (int)ltm;
            if (_total != -1 && _total != 0)
                _start = Environment.TickCount;
            else
                _start = 0;
        }

        public TimeoutTracker(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
            _total = millisecondsTimeout;
            if (_total != -1 && _total != 0)
                _start = Environment.TickCount;
            else
                _start = 0;
        }

        public int RemainingMilliseconds
        {
            get
            {
                if (_total == -1 || _total == 0)
                    return _total;

                int elapsed = Environment.TickCount - _start;
                // elapsed may be negative if TickCount has overflowed by 2^31 milliseconds.
                if (elapsed < 0 || elapsed >= _total)
                    return 0;

                return _total - elapsed;
            }
        }

        public bool IsExpired
        {
            get
            {
                return RemainingMilliseconds == 0;
            }
        }
    }
}
