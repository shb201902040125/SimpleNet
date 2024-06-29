using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNet
{
    internal static class BufferPool<T>
    {
        static Dictionary<int, (List<(T[],bool)>, int)> _buffers = [];
        static int GetFitSize(int size)
        {
            if (size < 256)
            {
                return 256;
            }
            else
            {
                size--;
                size |= size >> 1;
                size |= size >> 2;
                size |= size >> 4;
                size |= size >> 8;
                size |= size >> 16;
                size++;
                return size;
            }
        }
        public static T[] Rent(int size, bool clear = false)
        {
            int fitSize = GetFitSize(size);
            T[] buffer;
            if (_buffers.TryGetValue(fitSize, out (List<(T[] buffer, bool inUse)> buffers, int times) entry))
            {
                int ptr = -1;
                for (int i = 0; i < entry.buffers.Count; i++)
                {
                    if (!entry.buffers[i].inUse)
                    {
                        ptr = i;
                        break;
                    }
                }
                if (ptr == -1)
                {
                    buffer = ArrayPool<T>.Shared.Rent(fitSize);
                    entry.buffers.Add((buffer, true));
                    entry.times++;
                    goto Label;
                }
                (T[] buffer, bool inUse) _buffer = entry.buffers[ptr];
                buffer = _buffer.buffer;
                _buffer.inUse = true;
                entry.times++;
                goto Label;
            }
            else
            {
                buffer = ArrayPool<T>.Shared.Rent(fitSize);
                _buffers[fitSize] = ([(buffer, true)], 1);
                goto Label;
            }
        Label:;
            if(clear)
            {
                Array.Fill(buffer, default);
            }
            return buffer;
        }
        public static void Return(T[] buffer)
        {
            int fitSize = GetFitSize(buffer.Length);
            if (_buffers.TryGetValue(fitSize, out (List<(T[] buffer, bool inUse)> buffers, int times) entry))
            {
                int ptr = -1;
                for (int i = 0; i < entry.buffers.Count; i++)
                {
                    if (entry.buffers[i].buffer == buffer)
                    {
                        ptr = i;
                        break;
                    }
                }
                if (ptr == -1)
                {
                    return;
                }
                (T[] buffer, bool inUse) _buffer = entry.buffers[ptr];
                _buffer.inUse = false;
                entry.times--;
                if (entry.times == 0)
                {
                    foreach (var e in entry.buffers)
                    {
                        ArrayPool<T>.Shared.Return(e.buffer);
                    }
                    entry.buffers.Clear();
                    _buffers.Remove(fitSize);
                }
            }
        }
    }
}
