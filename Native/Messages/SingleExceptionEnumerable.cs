using System;
using System.Collections;
using System.Collections.Generic;

namespace Chopsticks.Messages
{
    /// <summary>
    /// A zero-allocation enumerable for a single exception.
    /// </summary>
    internal readonly struct SingleExceptionEnumerable : IEnumerable<Exception>
    {
        private readonly Exception _exception;
        
        public SingleExceptionEnumerable(Exception exception) => _exception = exception;
        
        public Enumerator GetEnumerator() => new(_exception);
        
        IEnumerator<Exception> IEnumerable<Exception>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public struct Enumerator : IEnumerator<Exception>
        {
            private readonly Exception _exception;
            private bool _moved;
            
            public Enumerator(Exception exception)
            {
                _exception = exception;
                _moved = false;
            }
            
            public Exception Current => _exception;
            object IEnumerator.Current => _exception;
            
            public bool MoveNext()
            {
                if (_moved)
                    return false;
                _moved = true;
                return true;
            }
            
            public void Reset() => _moved = false;
            public void Dispose() { }
        }
    }
}
