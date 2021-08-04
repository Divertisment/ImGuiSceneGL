using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImGuiSceneTest {
    public class FixedSizedLog :ConcurrentQueue<(string, int)> {
        private readonly object syncObject = new object();

        public int Size { get; private set; }
        public FixedSizedLog(int size) {
            Size = size;
        }
        public new void Clear() {
            lock(syncObject) {
                (string, int) outObj;
                while(base.TryDequeue(out outObj)) {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Adds an object to the end of the System.Collections.Concurrent.ConcurrentQueue`1.
        /// </summary>
        /// <param name="obj"></param>
        public void Add(string str) {// need to implement a handler for string with [value]
            var sampl = @"\[(?<asd>[^\[\]]*)\]";

            lock(syncObject) {
                var ci = 0;
                var index = -1;
                //looking for an old line, the same as the one we add,
                //discarding the changeable one inside the square brackets
                foreach(var c in this) { //
                    var curr = Regex.Replace(c.Item1, sampl, "");
                    var nstr = Regex.Replace(str, sampl, "");
                    if(curr == nstr) {
                        index = ci;
                        break;
                    }
                    ci++;
                }
                if(index != -1 && index < Size) { //if found old string...
                    var old = base.ToArray();
                    if(str.Contains("[") && str.Contains("]"))
                        old[index] = (str, 1);
                    else
                        old[index] = (old[index].Item1, old[index].Item2 + 1);

                    Clear();
                    foreach(var v in old)
                        base.Enqueue(v);
                }
                else { //add a new value to the end of the list and crop at the top
                    base.Enqueue((str, 1));
                    while(base.Count > Size) {
                        (string, int) outObj;
                        base.TryDequeue(out outObj);
                    }
                }
            }
        }
    }
}
