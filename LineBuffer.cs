using System.Collections.Generic;
using System.Text;
using System;

namespace GarbageIRC {
    public class LineBuffer {
        private List<byte> Data = new List<byte>();

        public bool LineAvailable { get => (IndexOfNextCRNL() != -1); }

        public LineBuffer() {

        }

        public void AddData(byte[] data, int count) {
            for (int i = 0; i < count; i++) {
                Data.Add(data[i]);
            }
        }

        private int IndexOfNextCRNL() {
            //  Do a linear search for \r\n.
            //  Slow, but meh.
            for (int i = 0; i < Data.Count; i++) {
                if (i + 1 < Data.Count && Data[i] == '\r' && Data[i + 1] == '\n') {
                    return i;
                }
            }

            return -1;
        }

        public string GetNextLine() {
            int idx = IndexOfNextCRNL();
            if (idx == -1) {
                return null;
            }

            List<byte> line = Data.GetRange(0, idx); // Up to CRNL.
            Data.RemoveRange(0, idx + 2); // + 2 to remove the CRNL.
            string ret = Encoding.UTF8.GetString(line.ToArray());

            return ret;
        }
    }
}