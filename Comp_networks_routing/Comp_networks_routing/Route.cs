using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comp_networks_routing
{
    class Route
    {
        internal Tuple<uint, uint> nodes;
        internal uint value;
        internal List<uint> points;

        static internal SortedDictionary<Tuple<uint, uint>, Route> RoutesTable;
        static internal SortedDictionary<Tuple<uint, uint>, Route> leastNodesRoutesTable;

        static Route()
        {
            RoutesTable = new SortedDictionary<Tuple<uint, uint>, Route>();
            leastNodesRoutesTable = new SortedDictionary<Tuple<uint, uint>, Route>();
        }

        static public Tuple<uint,uint> EnPair(uint v1, uint v2)
        {
            return new Tuple<uint, uint>(v1, v2);
        }

        public Route()
        {
            points = new List<uint>();
            value = 0;
        }
        public Route(uint id)
        {
            points = new List<uint>();
            points.Add(id);
            value = 0;
        }
        public Route(Route copy)
        {
            points = new List<uint>();
            foreach (var elem in copy.points) points.Add(elem);
            value = copy.value;
        }

        public void AddNode(uint id){ points.Add(id); }

        public void AddNode(uint id, uint value)
        {
            points.Add(id);
            this.value += value;
        }

        public Route Append(uint id, uint value)
        {
            var res = new Route(this);
            res.AddNode(id, value);
            return res;
        }

        public uint GetValue() { return value; }

        public bool IsPassed(uint nodeId)
        { return points.Exists((uint index) => index == nodeId);}

        public uint GetStart() { return points[0]; }
        public uint GetNextStep() { return points[1]; }
        public uint GetEnd(){ return points[points.Count - 1]; }
        public Tuple<uint,uint> GetPair()
        { return new Tuple<uint, uint>(GetStart(), GetEnd()); }
        public int GetLength() { return points.Count; }
        public List<uint> GetPath() { return points; }

        public override String ToString()
        {
            String res = "";
            res += $"<{GetStart()}, {GetEnd()}> => (";
            for (int i = 0; i < points.Count - 1; i++)
                res += $"{points[i]}, ";
            res += $"{points[points.Count - 1]}) = {value}";
            return res;
        }
    }
}
