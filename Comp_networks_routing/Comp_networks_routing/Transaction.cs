using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Comp_networks_routing
{
    class Transaction
    {
        static public Dictionary<uint, bool> BusyNodes = new Dictionary<uint, bool>();
        static public List<Edge> BusyEdges = new List<Edge>();
        static public List<Transaction> transactions = new List<Transaction>();
        static public List<Message> 
            LostMessages = new List<Message>(),
            SentMessages = new List<Message>();
        static public uint StepCount = 0;
        static public List<Tuple<uint, Message>> WaitingMessages = new List<Tuple<uint, Message>>();
        static public double AllTime = 0;

        internal Message message;
        internal uint from, to;
        List<Package> packages;
        int step = 0;
        int sending = 0;
        internal bool Ended = false;
        internal Edge edge;
        internal Func<double, Point?> slicer;
        internal bool inProcess;
        bool stored;
        internal MessageType type;

        public Transaction(Message m, uint from)
        {
            this.from = from;
            to = Route.RoutesTable[Route.EnPair(from, m.destination)].GetNextStep();
            message = m;
            packages = m.packages;
            m.creationTime = StepCount;
            Refresh();
            stored = false;
            type = message.type;
        }

        void LooseMessage()
        {
            Ended = true;
            message.endTime = StepCount;
            LostMessages.Add(message);
        }

        void EndMessage()
        {
            Ended = true;
            message.endTime = StepCount;
            SentMessages.Add(message);
            AllTime += message.delayTime;
            Form1.AverageTime = AllTime / SentMessages.Count;
        }

        public void NextStep()
        {
            if (to == message.destination)
            {
                EndMessage();
                if (inProcess)
                    FreeChannel();
                return;
            }
            else
            {
                uint buff = Route.RoutesTable[Route.EnPair(to, message.destination)].GetNextStep();
                if (inProcess)
                    FreeChannel();
                from = to;
                to = buff;
                Refresh();
            }
            step = 0;
        }

        void Refresh()
        {
            step = 0;
            sending = 0;
            edge = Form1.edges.Find(e => Edge.Compare(e.Dir, Route.EnPair(from, to)));
            slicer = edge.GetEdgeSlicer(from == edge.Dir.Item2);
            inProcess = false;
        }

        bool CheckChannel()
        {
            int ind = BusyEdges.FindIndex(el => el == edge);
            return ind < 0;
        }

        void TakeChannel()
        {
            BusyEdges.Add(edge);
        }

        bool FreeChannel()
        {
            int ind = BusyEdges.FindIndex(el => el == edge);
            if (ind < 0) return false;
            BusyEdges.RemoveAt(ind);
            return true;
        }

        public void Step()
        {
            if (!inProcess)
            {
                if (!CheckChannel())
                {
                    if (!stored)
                    {
                        stored = Form1.PointsMap[from].StoreMessage(message);
                    }
                    message.delayTime++;
                    return;
                }
                if (stored)
                {
                    if (Form1.PointsMap[from].RestoreMessage(message))
                        stored = false;
                    else
                    {
                        LooseMessage();
                        return;
                    }
                }
                inProcess = true;
                TakeChannel();
            }
            if (step >= packages.Count)
                NextStep();
            else
            {
                sending++;
                if (sending >= edge.value)
                {
                    sending = 0;
                    if (packages[step].type == PackageType.Ack ||
                        packages[step].type == PackageType.RR)
                        Form1.PointsMap[from].Receive(packages[step]);
                    else
                    {
                        if (!Form1.UdpProtocol && ((int)Edge.rand.Next(100) < edge.pError * 100))
                            return;
                    }
                        Form1.PointsMap[to].Receive(packages[step]);
                    step++;
                }
            }
        }

        public double GetProgres()
        {
            return ((step * edge.value) + sending) / ((double)packages.Count * edge.value);
        }
        public Point? GetProgresPoint() { return slicer(GetProgres()); }
    }
}
