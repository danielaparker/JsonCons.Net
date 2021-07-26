using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonCons.JsonPath
{
    readonly struct Slice
    {
        readonly Int32? _start;
        readonly Int32? _stop;

        public Int32 Step {get;}

        public Slice(Int32? start, Int32? stop, Int32 step) 
        {
            _start = start;
            _stop = stop;
            Step = step;
        }

        public Int32 GetStart(Int32 size)
        {
            if (_start.HasValue)
            {
                Int32 len = _start.Value >= 0 ? _start.Value : size + _start.Value;
                return len <= size ? len : size;
            }
            else
            {
                if (Step >= 0)
                {
                    return 0;
                }
                else 
                {
                    return size;
                }
            }
        }

        public Int32 GetStop(Int32 size)
        {
            if (_stop.HasValue)
            {
                Int32 len = _stop.Value >= 0 ? _stop.Value : size + _stop.Value;
                return len <= size ? len : size;
            }
            else
            {
                return Step >= 0 ? size : -1;
            }
        }
    };

} // namespace JsonCons.JsonPath
