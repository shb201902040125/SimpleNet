using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet
{
    public class BufferArray<T> : IDisposable
    {
        T[] _data;
        public BufferArray(int size)
        {
            _data = BufferPool<T>.Rent(size);
        }
        public BufferArray(T[] data, bool noCopy = true)
        {
            if(noCopy)
            {
                _data = data;
            }
            _data = BufferPool<T>.Rent(data.Length);
            Array.Copy(data, _data, data.Length);
        }
        public T this[int index]
        {
            get
            {
                return _data[index];
            }
            set
            {
                _data[index] = value;
            }
        }
        public void Dispose()
        {
            BufferPool<T>.Return(_data);
            GC.SuppressFinalize(this);
        }
        public static implicit operator T[](BufferArray<T> array) 
        {
            return array._data;
        }
    }
}
