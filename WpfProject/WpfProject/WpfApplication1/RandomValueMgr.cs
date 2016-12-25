using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace wpf2048App
{
    class RandomValueMgr
    {
        class History
        {
            private Random rand;
            private List<int> items = new List<int>();
            private int index = 0;

            public bool ReplayFinished { get { return index == items.Count; } }

            public History(Random rand)
            {
                this.rand = rand;
            }

            public void Reset()
            {
                index = 0;
            }

            public int Next(int max)
            {
                if (index == items.Count)
                {
                    return AddNew(max);
                }

                var retv = items[index++];
                Debug.Assert(retv < max);
                return retv; 
            }

            private int AddNew(int max)
            {
                int retv = rand.Next(max);
                items.Add(retv);
                index = items.Count;
                return retv;
            }
        }

        private Random rand = new Random();
        private Dictionary<string, History> history = new Dictionary<string, History>();

        public bool ReplayFinished { get { return GetReplayFinished(); } }

        public void Replay()
        {
            foreach (var h in history)
            {
                h.Value.Reset();
            }
        }

        public int Next(int max, [CallerMemberName]string sourceName = null)
        {
            if (!history.ContainsKey(sourceName))
            {
                history[sourceName] = new History(rand);
            }

            return history[sourceName].Next(max);
        }

        private bool GetReplayFinished()
        {
            foreach (var h in history)
            {
                if (!h.Value.ReplayFinished)
                {
                    return false;
                }
            }

            return true;
        }

    }
}
