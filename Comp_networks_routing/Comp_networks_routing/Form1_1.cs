using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Text;

namespace Comp_networks_routing
{
    public partial class Form1 : Form
    {
        bool Emulation = false;
        bool NetworkChanged = false;
        bool RouteGenerated = false;
        int steps = 0;
        bool autoSteps = false;
        static public bool UdpProtocol = true;
        bool SendHelloPath = false;
        bool HelloFinished = false;
        static public double AverageTime = -1;
        Dictionary<uint, Message> WaitingMessages;
        Dictionary<Tuple<uint,uint>, Message> SendingMessages;
        List<Transaction> transactions = new List<Transaction>();
        Dictionary<uint, List<uint>> NeiborNodes;
        Dictionary<uint, List<Edge>> NeiborEdges;
        Mutex mutex;
        Object lockObject;

        delegate void TextAppend(String str);
        TextAppend textAppend;

        delegate void SetEmulButtons(bool value);
        SetEmulButtons setEmulButtons;

        void Draw(Transaction tr, Graphics g)
        {
            if (!tr.inProcess) return;
            DrawEdge(tr.edge, g, Color.Red);
            Point? p = tr.GetProgresPoint();
            if (!(p is null))
            {
                g.FillRectangle(new SolidBrush(tr.type == MessageType.HelloPath? Color.Green : Color.Black), p.Value.X, p.Value.Y, 4,4);
            }

        }

        List<Edge> GetIncedentalEdges(uint nodeId)
        {
            List<Edge> res;
            if (NeiborEdges.TryGetValue(nodeId, out res)) return res;
            res = new List<Edge>();
            foreach (var edge in Edges)
            {
                if (edge.isEnabled() && !edge.isLoop() && edge.IsIncidental(nodeId))
                    res.Add(edge);
            }
            NeiborEdges[nodeId] = res;
            return res;
        }

        List<uint> GetNeiborNodes(uint nodeId)
        {
            List<uint> res = new List<uint>();
            List<Edge> _edges = GetIncedentalEdges(nodeId);
            foreach (var edge in _edges)
            {
                uint? id = edge.GetNeibor(nodeId);
                if(PointsMap[(uint) id].isEnabled())
                    res.Add((uint)id);
            }
            return res;
        }

        void FindPath(Route path, uint id)
        {
            List<Edge> edges = GetIncedentalEdges(id);
            if (path.GetLength() > 1)
            {
                Tuple<uint, uint> pair = path.GetPair();
                if (!Route.RoutesTable.ContainsKey(pair) ||
                    Route.RoutesTable[pair].GetValue() > path.GetValue())
                    Route.RoutesTable[pair] = path;
                if (!Route.leastNodesRoutesTable.ContainsKey(pair) ||
                    Route.leastNodesRoutesTable[pair].GetLength() > path.GetLength())
                    Route.leastNodesRoutesTable[pair] = path;
            }
            foreach (var elem in edges)
            {
                var neibor = (uint) elem.GetNeibor(id);
                if (!path.IsPassed(neibor))
                    FindPath(path.Append(neibor, elem.value), neibor);
            }
        }

        void DijkstraAlg()
        {
            NeiborEdges.Clear();
            NeiborNodes.Clear();
            foreach (var elem in PointsMap)
            {
                FindPath(new Route(elem.Key), elem.Key);
            }
        }

        Thread CreateTables()
        {
            Thread thread = new Thread(() => {
                richTextBox1.BeginInvoke(textAppend, "Start of minimum path searching\n");
                DijkstraAlg();
                richTextBox1.BeginInvoke(textAppend, "End of minimum path searching\n");
            });
            thread.Start();
            return thread;
        }

        void SetEnableForEmulation(bool value)
        {
            Generate.Enabled = value;
            Clear.Enabled = value;
            Printpaths.Enabled = value;
            button3.Enabled = !value;
            button4.Enabled = value;
            groupBox1.Enabled = value;
            groupBox2.Enabled = value;
            groupBox3.Enabled = value;
            groupBox4.Enabled = value;
            Send.Enabled = !value;
            SendRandom.Enabled = !value;
            AutoSteps.Enabled = !value;
            button3.Enabled = !value;
        }

        void SetEmulationButtons(bool value)
        {
            Send.Enabled = value;
            SendRandom.Enabled = value;
            AutoSteps.Enabled = value;
            button3.Enabled = value;
        }
        void SetSendButtons(bool value)
        {
            Send.Enabled = value;
            SendRandom.Enabled = value;
        }

        void StartEmulation()
        {
            Transaction.WaitingMessages.Clear();
            Transaction.transactions.Clear();
            ClearAllNodesStats();
            SetEnableForEmulation(false);
            Transaction.StepCount = 0;
            SendHelloPath = true;
            Transaction.AllTime = 0;

        }

        void EndEmulation()
        {
            SetEnableForEmulation(true);
            Transaction.WaitingMessages.Clear();
            autoSteps = false;
            Transaction.transactions.Clear();
            Transaction.StepCount = 0;
            ShowStatistic();
            Transaction.AllTime = 0;
        }

        void SendHelloPackages()
        {
            foreach(var pair in pointsMap)
            {
                List<uint> neibors = GetNeiborNodes(pair.Key);
                foreach (var i in neibors)
                    Transaction.WaitingMessages.Add(new Tuple<uint, Message>(pair.Key, new Message(UdpProtocol, 20, MessageType.HelloPath, i)));
            }
        }

        void CreateRandomMessage(int minSize, int maxSize)
        {
            if (maxSize <= minSize) return;
            uint n1, n2;
            n1 = (uint)Edge.rand.Next(31) + 1;
            n2 = (uint)Edge.rand.Next(31) + 1;
            while (n2 == n1)
                n2 = (uint)Edge.rand.Next(31) + 1;
            Transaction.WaitingMessages.Add(new Tuple<uint, Message>(n1, new Message(UdpProtocol, Edge.rand.Next(maxSize - minSize) + minSize, MessageType.Data, n2)));
        }

        void GenerateMessage()
        {
            if (!checkBox6.Checked) return;
            if(Edge.rand.Next(100) < (int)numericUpDown17.Value)
            {
                CreateRandomMessage((int)numericUpDown19.Value, (int)numericUpDown18.Value);
            }
        }

        void ClearAllNodesStats()
        {
            foreach(var pair in PointsMap)
            {
                pair.Value.ClearStat();
            }
        }

        void ShowStatistic()
        {
            uint time = 0, delayTime = 0;
            uint count = 0;
            foreach (var m in Transaction.SentMessages)
            {
                delayTime += m.delayTime;
                time += m.endTime - m.creationTime;
                count++;
            }
            double avTime = time / (double)count;
            double avDelay = delayTime / (double)count;
            richTextBox1.BeginInvoke(textAppend, $"\n\n\nAvarage time for delivering message: {avTime}\nAvarage time of delay: {avDelay}");

            count = 0;
            double KB = 0, packages = 0, ServiceKB = 0, ServicePackages = 0;
            foreach(var pair in PointsMap)
            {
                KB += pair.Value.ReceivedKB;
                packages += pair.Value.ReceivedPackages;
                ServiceKB += pair.Value.ReceivedServiceKB;
                ServicePackages += pair.Value.ReceivedServicePackages;
                count++;
            }
            KB /= count;
            packages /= count;
            ServiceKB /= count;
            ServicePackages /= count;
            richTextBox1.BeginInvoke(textAppend, $"\n\nAvarage:\n Inf KB: {KB}\n Inf Packages: {packages}\n Service KB: {ServiceKB*0.35}\n Sevice packages: {ServicePackages}\n Count: " +
                $"{Transaction.SentMessages.Count}\n\n");
            Transaction.SentMessages.Clear();
        }

        void GlobalStep()
        {
            if (SendHelloPath)
            {
                SendHelloPackages();
                SendHelloPath = false;
            }
            List<Tuple<uint, Message>> del = new List<Tuple<uint, Message>>();
            lock (lockObject)
            {
                foreach (var pair in Transaction.WaitingMessages)
                {
                    Transaction.transactions.Add(new Transaction(pair.Item2, pair.Item1));
                    del.Add(pair);
                }
            }
            foreach (var i in del) Transaction.WaitingMessages.Remove(i);
            foreach (var tr in Transaction.transactions) tr.Step();
            lock (lockObject)
            {
                Transaction.transactions.RemoveAll(tr => tr.Ended);
            }
            RedrawGraph();
            if (HelloFinished)
                GenerateMessage();
            else
            if (Transaction.transactions.FindIndex(el => el.message.type == MessageType.HelloPath) < 0)
            {
                Send.BeginInvoke((Action)(() => { SetSendButtons(true); }));
                HelloFinished = true;
            }
            //richTextBox1.BeginInvoke(textAppend, $"Performing step : {Transaction.StepCount}\n");
            Transaction.StepCount++;
        }
    }
}
