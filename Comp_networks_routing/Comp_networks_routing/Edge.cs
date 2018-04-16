using System;
using System.Collections.Generic;
using System.Drawing;

namespace Comp_networks_routing
{
    public enum Type { duplex, semiduplex };

    class Edge
    {
        static uint[] Values;
        public static Random rand = new Random();
        
        static Edge()
        {
            Values = new uint[13] { 1, 2, 4, 6, 7, 9, 10, 12, 15, 18, 21, 22, 25 };
        }
        internal Tuple<uint,uint> Dir;
        internal uint value;
        internal int index;
        internal int type;
        internal double pError;
        internal bool enabled;


        public Edge(int ind)
        {
            index = rand.Next(13);
            value = Values[index];
            type = rand.Next(0,2);
            pError = 1 - (double)rand.Next(90,100)/100;
            index = ind;
            enabled = true;
        }

        public Edge(uint val, int tp, int err,Tuple<uint,uint> dir,int ind)
        {
            value = val;
            type = tp;
            pError = 1 - (double)err /100;
            Dir = dir;
            index = ind;
            enabled = true;
        }

        public bool IsIncidental(uint nodeId)
        { return (nodeId == Dir.Item1 || nodeId == Dir.Item2); }

        public bool isLoop(){ return Dir.Item1 == Dir.Item2; }

        public uint? GetNeibor(uint id)
        {
            if (id == Dir.Item1) return Dir.Item2;
            if (id == Dir.Item2) return Dir.Item1;
            return null;
        }

        internal static List<Edge> GenerateRandomNet(double averPow, int quant, int nets)
        {
            Random rnd = new Random();
            var EdgeTable = new Dictionary<Tuple<uint, uint>, Edge>();
            var EdgeList = new List<Edge>();
            int id = 0;
            for (int k = 0; k < nets*quant; k+=quant)
            {
                int i = 1;
                Edge buf = new Edge(id);
                buf.Dir = new Tuple<uint, uint>((uint)(i % quant + 1+k), (uint)rnd.Next(1+k, quant + 1+k));
                while (i <= CalculateEdgesQuant(averPow, quant))
                {
                    var res = EdgeList.Find(el => buf == el);
                    if (!(buf.Dir.Item1 == buf.Dir.Item2) &
                        (res is null))
                    {
                        EdgeList.Add(buf);
                        i++;
                        id++;
                    }
                    buf = new Edge(id);
                    buf.Dir = new Tuple<uint, uint>((uint)(i % quant + 1+k), (uint)rnd.Next(1+k, quant + 1+k));
                }
            }
            return EdgeList;
        }

        public Func<double,Point?> GetEdgeSlicer(bool reverse)
        {
            return (double p) =>
            {
                Point p1 = Form1.PointsMap[Dir.Item1].coordinates;
                Point p2 = Form1.PointsMap[Dir.Item2].coordinates;
                if (p < 0 || p > 1) return null;
                double div1 = (p2.X - p1.X);
                double div2 = (p2.Y - p1.Y);
                int x, y;
                if(div1 == 0)
                {
                    x = p1.X;
                    y = reverse ? (int)(p2.Y - div2 * p) : (int)(p1.Y + div2 * p);
                    return new Point(x, y);
                }
                if (div2 == 0)
                {
                    y = p1.Y;
                    x = reverse ? (int)(p2.X - div1 * p) : (int)(p1.X + div1 * p);
                    return new Point(x, y);
                }  
                x = reverse ? (int)(p2.X - div1 * p) : (int)(p1.X + div1 * p);
                y = (int)(((x - p1.X) * div2 / div1) + p1.Y);
                return new Point(x, y);
            };
        }

        internal static int CalculateEdgesQuant(double averPow, int quant)
        {
            return ((int)(averPow * quant)) / 2;
        }

        public static bool operator ==(Edge op1, Edge op2)
        {
           return op1.Dir.Item1 == op2.Dir.Item1 && op1.Dir.Item2 == op2.Dir.Item2 ||
                op1.Dir.Item1 == op2.Dir.Item2 && op1.Dir.Item2 == op2.Dir.Item1;
        }

        public static bool operator !=(Edge op1, Edge op2)
        {
            return !(op1 == op2);
        }

        public override bool Equals(object other)
        {
            if (!(other is Edge)) return false;
            return this == (Edge)other;
        }

        public override int GetHashCode()
        {
            return Dir.GetHashCode();
        }

        static Tuple<uint,uint> Reverse(Tuple<uint,uint> tuple)
        {
            return new Tuple<uint, uint>(tuple.Item2, tuple.Item1);
        }

        public static bool Compare(Tuple<uint,uint> t1, Tuple<uint, uint> t2)
        {
            return t1.Item1 == t2.Item1 && t1.Item2 == t2.Item2 ||
                t1.Item1 == t2.Item2 && t1.Item2 == t2.Item1;
        }

        public void TurnOn() { enabled = true; }
        public void TurnOff() { enabled = false; }
        public bool isEnabled() { return enabled; }
    }
}
