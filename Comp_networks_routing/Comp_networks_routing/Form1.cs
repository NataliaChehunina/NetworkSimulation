using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Comp_networks_routing
{
    public partial class Form1 : Form
    {
        Graphics g;
        List<Node> Points,OffPoints;
        private static List<Edge> Edges,OffEdges;
        private static Dictionary<uint, Node> pointsMap = new Dictionary<uint, Node>();
        Point Buff;
        uint Index;
        bool Flag, AddClick = false;
        Brush pointColor,selectedPointColor,Wpoint,Bpoint,DisabledColor;
        Font drawFont;
        Pen pen;
        Thread stepper;

        internal static Dictionary<uint, Node> PointsMap { get => pointsMap; set => pointsMap = value; }
        internal static List<Edge> edges { get => Edges; set => Edges = value; }
        internal static List<Edge> offEdges { get => OffEdges; set => OffEdges = value; }

        public Form1()
        {
            InitializeComponent();
        }

        ~Form1()
        {
            autoSteps = false;
            //stepper.Abort();
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            g = Graphics.FromHwnd(pictureBox1.Handle);
            Points = new List<Node>();
            OffPoints = new List<Node>();
            OffEdges = new List<Edge>();
            //PointsMap = new Dictionary<uint, Node>();
            Edges = new List<Edge>();
            Flag = false;
            pointColor = new SolidBrush(Color.FromArgb(255, 200, 200, 100));
            selectedPointColor = new SolidBrush(Color.Blue);
            Bpoint = new SolidBrush(Color.Black);
            Wpoint = new SolidBrush(Color.White);
            DisabledColor = new SolidBrush(Color.Yellow);
            drawFont = new Font("Arial", 10);
            pen = new Pen(Bpoint);
            WaitingMessages = new Dictionary<uint, Message>();
            NeiborNodes = new Dictionary<uint, List<uint>>();
            NeiborEdges = new Dictionary<uint, List<Edge>>();

            pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.RedrawEvent);
            textAppend = new TextAppend(this.AppendText);
            setEmulButtons = new SetEmulButtons(this.SetEmulationButtons);
            mutex = new Mutex();
            lockObject = new Object();

            this.FormClosing += new FormClosingEventHandler((object obj, FormClosingEventArgs ev) => 
            {
                autoSteps = false;
            });
            GenerateNetwork();
        }

        private void AppendText(String str)
        {
            richTextBox1.Text += str;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            AddClick = true;
            NetworkChanged = true;
            if(checkBox1.Checked == true)
            {
                if (IsPointExists((int)numericUpDown1.Value, Points))
                {
                    uint index = (uint)numericUpDown1.Value;
                    PointsMap[index].netId = (uint)numericUpDown2.Value;
                    PointsMap[index].bufferSize = (int)numericUpDown12.Value;
                    //CreateTables();
                }
                else
                    MessageBox.Show("Wrong point ID!!!",
                "Error",
                MessageBoxButtons.OK);
            }    
        }

        void AddAdditionalPoints()
        {
            int BufferSize = 2000;
            Point point = new Point(400,280);
            Node node = new Node(5, point, BufferSize);
            Points.Add(node);
            PointsMap.Add(node.id,node);
            
            point = new Point(490,280);
            Node node2 = new Node(5, point, BufferSize);
            Points.Add(node2);
            PointsMap.Add(node2.id, node2);
            
            point = new Point(400, 380);
            Node node3 = new Node(5, point, BufferSize);
            Points.Add(node3);
            PointsMap.Add(node3.id, node3);
            
            point = new Point(490, 380);
            Node node4 = new Node(5, point, BufferSize);
            Points.Add(node4);
            PointsMap.Add(node4.id, node4);

            point = new Point(310, 330);
            Node sat = new Node(6, point, BufferSize);
            Points.Add(sat);
            PointsMap.Add(sat.id, sat);

            point = new Point(580, 330);
            Node sat2 = new Node(6, point, BufferSize);
            Points.Add(sat2);
            PointsMap.Add(sat2.id, sat2);

            Edges.Add(new Edge(21, 0, 98, new Tuple<uint, uint>(12, node3.id), Edges.Count));
            Edges.Add(new Edge(22, 0, 96, new Tuple<uint, uint>(21, node2.id), Edges.Count));
            Edges.Add(new Edge(22, 0, 95, new Tuple<uint, uint>(node.id, node2.id), Edges.Count));
            Edges.Add(new Edge(25, 0, 94, new Tuple<uint, uint>(8, node.id), Edges.Count));
            Edges.Add(new Edge(21, 0, 97, new Tuple<uint, uint>(25, node4.id), Edges.Count));
            Edges.Add(new Edge(22, 0, 99, new Tuple<uint, uint>(node3.id, node4.id), Edges.Count));
            Edges.Add(new Edge(40, 0, 97, new Tuple<uint, uint>(sat.id, node3.id), Edges.Count));
            Edges.Add(new Edge(40, 0, 95, new Tuple<uint, uint>(sat.id, node.id), Edges.Count));
            Edges.Add(new Edge(40, 0, 97, new Tuple<uint, uint>(sat2.id, node2.id), Edges.Count));
            Edges.Add(new Edge(40, 0, 96, new Tuple<uint, uint>(sat2.id, node4.id), Edges.Count));
        }

        void DrawPointAux(int x, int y, uint id, Brush brush, Graphics g)
        {   
            g.FillEllipse(brush, x - 10, y - 10, 20, 20);
            g.DrawString(id.ToString(), drawFont, Bpoint, new Point(x - 8, y - 10));
        }

        void DrawPoint(int x, int y,uint id, Graphics g)
        {
            if(PointsMap[id].isEnabled())
                DrawPointAux(x, y, id, pointColor, g);
            else
                DrawPointAux(x, y, id, DisabledColor, g);
        }

        void DrawPointId(uint id, Graphics g)
        {
            var Point = PointsMap[id];
            DrawPointAux(Point.coordinates.X, Point.coordinates.Y, id, pointColor, g);
        }

        void SelectPoint(uint index, Graphics g)
        {
            var point = PointsMap[index];
            DrawPointAux(point.coordinates.X, point.coordinates.Y, index, selectedPointColor, g);
        }

        void DeletePoint(int x, int y, Graphics g)
        {
            g.FillEllipse(Wpoint, x - 10, y - 10, 20, 20);
        }

        void DrawAdditionalPoint(int x, int y, Graphics g)
        {
            g.FillEllipse(new SolidBrush(Color.Blue), x - 15, y - 15, 30, 30);
        }

        void DrawSat(int x, int y, Graphics g)
        {
            g.FillEllipse(new SolidBrush(Color.DarkRed), x - 15, y - 15, 30, 30);
        }

        void DeleteAdditionalPoint(int x, int y, Graphics g)
        {
            g.FillEllipse(Wpoint, x - 15, y - 15, 30, 30);
        }

        void DrawEdge(Point x,Point y, Graphics g)
        { 
            g.DrawLine(pen, x, y);
        }

        void DrawEdge(Edge edge, Graphics g)
        {
            var x = PointsMap[edge.Dir.Item1].coordinates;
            var y = PointsMap[edge.Dir.Item2].coordinates;
            if (edge.isEnabled())
                g.DrawLine(pen, x, y);
            else
                g.DrawLine(new Pen(Color.Yellow), x, y);
        }

        void DrawEdge(Edge edge, Graphics g, Color color)
        {
            var x = PointsMap[edge.Dir.Item1].coordinates;
            var y = PointsMap[edge.Dir.Item2].coordinates;
            g.DrawLine(new Pen(color), x, y);
        }

        void ChangeEdgeColor(Point x, Point y, Graphics g)
        {
            g.DrawLine(new Pen(Color.Yellow), x, y);
        }

        void ChangePointColor(int x, int y, uint id, Graphics g)
        {
            g.FillEllipse(new SolidBrush(Color.Yellow), x - 10, y - 10, 20, 20);
            g.DrawString(id.ToString(), drawFont, Bpoint, new Point(x - 8, y - 10));
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            switch(e.Button)
            {
                case MouseButtons.Right:
                    {
                        if (AddClick == true)
                        {
                            if (numericUpDown1.Enabled == false)
                            {
                                var node = new Node((uint)numericUpDown2.Value,
                                new Point(e.X, e.Y),(int) numericUpDown12.Value);
                                Points.Add(node);
                                PointsMap.Add(node.id,node);
                                RedrawGraph();
                                AddClick = false;
                                //CreateTables();
                            }
                        }
                        break;
                    }
                case MouseButtons.Left:
                    {
                        Node point = IsLocated(e.X, e.Y);
                        if (point != null)
                        {
                            Flag = true;
                            SelectPoint(point.id, g);
                            Index = point.id;
                            Buff = e.Location;
                        }
                               
                        break;
                    }
                case MouseButtons.Middle:
                    {
                        Node point = IsLocated(e.X, e.Y);
                        if(point != null)
                        {
                            MessageBox.Show(point.ToString(), "Point parameters", MessageBoxButtons.OK);
                            return;
                        }
                        int edgeId = IsFocusedOnEdge(e.X, e.Y);
                        if(edgeId>-1)
                        {    
                            Edge elem = Edges[edgeId];
                            MessageBox.Show(String.Format("Information about edge:\n ID: {0} \n Value: {1} \n " +
                                "Points: {2}<-->{3} \n Type: {4} \n Error probability: {5}\n Enabled : {6}",
                                elem.index,elem.value,elem.Dir.Item1,elem.Dir.Item2,
                                (Type)elem.type,elem.pError, elem.isEnabled()),
                                "Edge parameters", MessageBoxButtons.OK);
                        }
                        break;
                    }
            }    
        }

        Node IsLocated(int x, int y)
        {
            foreach(var elem in PointsMap)
            {
                if (Math.Abs(elem.Value.coordinates.X - x) <= 12 && 
                    Math.Abs(elem.Value.coordinates.Y - y) <= 12)
                    return elem.Value;
            }
            return null;
        }

        bool Nearby(double num1, double num2)
        {
            return Math.Abs(num1 - num2) <= 1;
        }

        int IsFocusedOnEdge(int x, int y)
        {
            for (int i = 0; i < Edges.Count; i++)
            {
                Point p1 = PointsMap[Edges[i].Dir.Item1].coordinates;
                Point p2 = PointsMap[Edges[i].Dir.Item2].coordinates;
                if (x < Math.Min(p1.X, p2.X)-3 ||
                    x > Math.Max(p1.X, p2.X)+3 ||
                    y < Math.Min(p1.Y, p2.Y)-3 ||
                    y > Math.Max(p1.Y, p2.Y)+3)
                    continue;
                double div1 = (p2.X - p1.X);
                if (div1 == 0 && Nearby(p1.X, x))
                    return i;
                double div2 = (p2.Y - p1.Y);
                if (div2 == 0 && Nearby(p1.Y, y))
                    return i;
                if (Math.Abs((x - p1.X) / div1 - (y - p1.Y) / div2) <= 0.07)
                    return i;
            }
            return -1;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Flag == true)
            {
                Flag = !Flag;
                PointsMap[Index].coordinates = e.Location;
                RedrawGraph();
            }
        }

        private void ClearAll()
        {
            Node.RestartCounter();
            Points.Clear();
            PointsMap.Clear();
            if(Edges != null)
                Edges.Clear();
            RedrawGraph();
        }

        private void GenerateNetwork()
        {
            ClearAll();
            Edges = Edge.GenerateRandomNet(3.5, 8, 4);
            ListNodeInit(8, 4);
            AddAdditionalPoints();
            RedrawGraph();
            NetworkChanged = true;
        }

        private void Generate_Click(object sender, EventArgs e)
        {
            GenerateNetwork();
            //CreateTables();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            ClearAll();
        }

        private void button1_Click(object sender, EventArgs e)//AddEdge
        {
            NetworkChanged = true;
            if(checkBox2.Checked == false)
            {
                uint p1 = (uint)numericUpDown4.Value;
                uint p2 = (uint)numericUpDown6.Value;
                if (PointsMap.ContainsKey(p1) 
                    && PointsMap.ContainsKey(p2) 
                    && p1 != p2 )
                {
                    Edge elem = new Edge(
                        (uint)numericUpDown5.Value,
                        comboBox1.SelectedIndex,
                        (int)numericUpDown8.Value,
                        new Tuple<uint,uint>(p1,p2),
                        Edges.Count);
                    if (Edges.IndexOf(elem) > 0)
                        MessageBox.Show("This edge is already exist!!!",
                        "Error",
                        MessageBoxButtons.OK);
                    Edges.Add(elem);
                    RedrawGraph();
                    //CreateTables();
                }
                else
                    MessageBox.Show("Wrong points!!!",
                        "Error",
                        MessageBoxButtons.OK);
            }
            else
            {
                int ind = (int)numericUpDown3.Value;
                if (IsEdgeExists(ind,Edges))
                {
                    Edge buf = Edges.Find(x=>x.index == ind);
                    buf.value = (uint)numericUpDown5.Value;
                    buf.type = comboBox1.SelectedIndex;
                    buf.pError = 1 - (double)numericUpDown8.Value/100;
                }
                else
                    MessageBox.Show("Wrong edge ID!!!",
                        "Error",
                        MessageBoxButtons.OK);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                numericUpDown3.Enabled = true;
                numericUpDown4.Enabled = false;
                numericUpDown6.Enabled = false;
            }
                
            else
            {
                numericUpDown3.Enabled = false;
                numericUpDown4.Enabled = true;
                numericUpDown6.Enabled = true;
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            NetworkChanged = true;
            if(checkBox3.Checked == false)//delete edge
            {
                if(IsEdgeExists((int)numericUpDown7.Value,Edges))
                {
                    DeleteEdge((int)numericUpDown7.Value);
                    //CreateTables();
                } 
                else
                    MessageBox.Show("Wrong edge ID!!!",
                        "Error",
                        MessageBoxButtons.OK);
            }
            else
            {
                if (IsPointExists((int)numericUpDown9.Value,Points))
                {
                    DeletePoint((uint)numericUpDown9.Value);
                    //CreateTables();
                }
                else
                    MessageBox.Show("Wrong point ID!!!",
                        "Error",
                        MessageBoxButtons.OK);
            }

        }

        void DeleteEdge(int id)
        {
            Edges.Remove(Edges.Find(x => x.index == id));
            RedrawGraph();
            //CreateTables();
        }

        void DeletePoint(uint id)
        {
            Node buf = PointsMap[id];
            int i = 0;
            while(i<Edges.Count)
            {
                var elem = Edges[i];
                if (elem.Dir.Item1 == buf.id || elem.Dir.Item2 == buf.id)
                {
                    DeleteEdge(elem.index);
                    continue;
                }
                i++;     
            }
            DeletePoint(buf.coordinates.X,buf.coordinates.Y, g);
            Points.Remove(Points.Find(x => x.id == id));
            PointsMap.Remove(id);
            //CreateTables();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {
                numericUpDown7.Enabled = false;
                numericUpDown9.Enabled = true;
            }  
            else
            {
                numericUpDown7.Enabled = true;
                numericUpDown9.Enabled = false;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {
                numericUpDown11.Enabled = false;
                numericUpDown10.Enabled = true;
            }
            else
            {
                numericUpDown11.Enabled = true;
                numericUpDown10.Enabled = false;
            }
        }

        private void ONOFF_Click(object sender, EventArgs e)//on
        {
            if (checkBox4.Checked == false)
            {
                OnEdge((int)numericUpDown11.Value);
            }
            else
            {
                OnPoint((uint)numericUpDown10.Value);
            }
            NetworkChanged = true;
        }

        private void button2_Click(object sender, EventArgs e)//off
        {
            if(checkBox4.Checked == false)
            {
                OffEdge((int)numericUpDown11.Value);
            }
            else
            {
                OffPoint((uint)numericUpDown10.Value);
            }
            NetworkChanged = true;
        }

        void OffPoint(uint ind)
        {
            //Node buf = Points.Find(x => x.id == ind);
            //Node buf = PointsMap.ContainsKey;
            if (PointsMap.ContainsKey(ind))
            {
                Node buf = PointsMap[ind];
                buf.TurnOff();
                int i = 0;
                while (i < Edges.Count)
                {
                    var elem = Edges[i];
                    if (elem.Dir.Item1 == buf.id || elem.Dir.Item2 == buf.id)
                    {
                        OffEdge(elem.index);
                    }
                    i++;
                }
                OffPoints.Add(buf);
                Points.Remove(Points.Find(x => x.id == ind));
                RedrawGraph();
               // CreateTables();
            }
            else
                MessageBox.Show("Wrong point ID!!!",
                    "Error",
                    MessageBoxButtons.OK);
        }

        void OnPoint(uint ind)
        {
            //Node buf = OffPoints.Find(x => x.id == ind);
            if (PointsMap.ContainsKey(ind) && !PointsMap[ind].isEnabled())
            {
                Node buf = PointsMap[ind];
                buf.TurnOn();
                int i = 0;
                while (i < Edges.Count)
                {
                    var elem = Edges[i];
                    if (elem.Dir.Item1 == buf.id || elem.Dir.Item2 == buf.id)
                    {
                        OnEdge(elem.index);
                        //continue;
                    }
                    i++;
                }
                Points.Add(buf);
                OffPoints.Remove(OffPoints.Find(x => x.id == ind));
                RedrawGraph();
                //CreateTables();
            }
            else
                MessageBox.Show("Wrong point ID!!!",
                    "Error",
                    MessageBoxButtons.OK);
        }

        void OffEdge(int id)
        {
            Edge buf = Edges.Find(x => x.index == id);
            if(!(buf is null))
            {
                buf.TurnOff();
                //Edges[id].TurnOff();
                RedrawGraph();
            }
            else
                MessageBox.Show("Wrong edge ID!!!",
                    "Error",
                    MessageBoxButtons.OK);
        }

        void OnEdge(int id)
        {
            Edge buf = Edges.Find(x => x.index == id);
            if (!(buf is null))
            {
                //Edges.Add(buf);
                //OffEdges.Remove(buf);
                if (PointsMap[buf.Dir.Item1].isEnabled() &&
                    PointsMap[buf.Dir.Item2].isEnabled())
                    buf.TurnOn();
                RedrawGraph();
                //CreateTables();
            }
            else
                MessageBox.Show("Wrong edge ID!!!",
                    "Error",
                    MessageBoxButtons.OK);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
                numericUpDown1.Enabled = true;
            else
                numericUpDown1.Enabled = false;
        }

        void AddEdge(uint id1, uint id2)
        {
            if (PointsMap.ContainsKey(id1) &&
                PointsMap.ContainsKey(id2))
            {
                Edge buf = new Edge(Edges.Count);
                buf.Dir = new Tuple<uint, uint>(id1, id2);
                Edges.Add(buf);
                //CreateTables();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < (int) numericUpDown13.Value; i++)
                GlobalStep();
        }

        private void Printpaths_Click(object sender, EventArgs e)
        {
            StringBuilder buf = new StringBuilder();
            FileStream fs = new FileStream("tables.txt", FileMode.Create);
            using (StreamWriter table = new StreamWriter(fs))
            {
                foreach (var pair in Route.RoutesTable)
                {
                    buf.Clear();
                    buf.Append($"{pair.Value}\n");
                    buf.Append($"{Route.leastNodesRoutesTable[pair.Key]}\n ----\n");
                    table.Write(buf.ToString());
                }
                table.WriteLine(buf.ToString());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CreateTables();
        }

        bool IsEdgeExists(int ind,List<Edge> list)
        {
            if(list.Find(x=>x.index == ind) is null)
                return false;
            return true;
        }

        bool IsPointExists(int ind, List<Node> list)
        {
            if (list.Find(x => x.id == ind) is null)
                return false;
            return true;
        }

        private void Send_Click(object sender, EventArgs e)
        {
            uint n1, n2;
            n1 = (uint)numericUpDown14.Value;
            n2 = (uint)numericUpDown15.Value;
            if (n1 != n2)
            {
                int size = (int) numericUpDown16.Value;
                Transaction.WaitingMessages.Add(new Tuple<uint, Message>(n1, new Message(UdpProtocol, size, MessageType.Data, n2)));
            }
        }

        private void SendRandom_Click(object sender, EventArgs e)
        {
            uint n1, n2;
            n1 = (uint)Edge.rand.Next(31) + 1;
            n2 = (uint)Edge.rand.Next(31) + 1;
            while (n2 == n1)
                n2 = (uint)Edge.rand.Next(31) + 1;
            Transaction.WaitingMessages.Add(new Tuple<uint, Message>(n1, new Message(UdpProtocol, Edge.rand.Next(100) + 40, MessageType.Data, n2)));
        }

        private void AutoSteps_Click(object sender, EventArgs e)
        {
            if (autoSteps)
            {
                autoSteps = false;
                //stepper.Abort();
                AutoSteps.BackColor = Color.Beige;
            }
            else
            {
                autoSteps = true;
                //if (!(stepper is null) && stepper.IsAlive)
                //    stepper.Abort();
                stepper = new Thread(() =>
                {
                    while (autoSteps)
                    {
                        GlobalStep();
                        Thread.Sleep(1);
                    }
                });
                stepper.Start();
                AutoSteps.BackColor = Color.Goldenrod;
            }
        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown15_ValueChanged(object sender, EventArgs e)
        {

        }

        private void EmulationOnOff_Click(object sender, EventArgs e)
        {
            UdpProtocol = checkBox5.Checked;
            if (Emulation)
                EndEmulation();
            else
                StartEmulation();
            Emulation = !Emulation;
            RedrawGraph();
        }

        void RedrawGraph()
        {
            pictureBox1.Invalidate();
        }

        private void RedrawEvent(object sender, PaintEventArgs e)
        {
            RedrawGraphAux(e.Graphics);
        }

        void RedrawGraphAux(Graphics g)
        {
            foreach (var elem in Edges)
                DrawEdge(elem, g);
            if (OffEdges.Count > 0)
            {
                foreach (var elem in OffEdges)
                    ChangeEdgeColor(PointsMap[elem.Dir.Item1].coordinates,
                        PointsMap[elem.Dir.Item2].coordinates, g);
            }
            foreach (var pair in PointsMap)
            {
                var elem = pair.Value;
                switch (elem.netId)
                {
                    case 5:
                        DrawAdditionalPoint(elem.coordinates.X, elem.coordinates.Y, g);
                        break;
                    case 6:
                        DrawSat(elem.coordinates.X, elem.coordinates.Y, g);
                        break;
                    default:
                        DrawPoint(elem.coordinates.X, elem.coordinates.Y, elem.id, g);
                        break;
                }
            }
            lock (lockObject)
            {
                foreach (Transaction tr in Transaction.transactions)
                {
                    Draw(tr, g);
                }
            }
        }

        void ListNodeInit(int quant, uint nets)
        {
            Node node;
            for (uint k = 1; k <= nets; k++)
            {
                for (int i = 0; i < quant; i++)
                {
                    if(k==1||k==3)
                    {
                        node = NewPointHeight(quant, i, k);
                    }
                    else
                    {
                        node = NewPointWidth(quant, i, k); 
                    }
                    Points.Add(node);
                    PointsMap[node.id] = node;
                }
            }
        }

        Node NewPointHeight(int quant,int i, uint k)
        {
            if(k==1)
            {
                if (i < quant / 2)
                    return new Node(k, new Point(i * 120 + 30, 30));
                return new Node(k, new Point((i % (quant / 2)) * 120 + 30, 230));
            }
            else
            {
                if (i < quant / 2)
                    return new Node(k, new Point(i * 120 + 490, 30));
                return new Node(k, new Point((i % (quant / 2)) * 120 + 490, 230));
            }
           
        }

        Node NewPointWidth(int quant, int i, uint k)
        {
            if(k==4)
            {
                if (i < quant / 2)
                    return new Node(k, new Point(i * 120 + 490, 30 + 400));
                return new Node(k, new Point((i % (quant / 2)) * 120 + 490, 230 + 400));
            }
            if (i < quant / 2)
                return new Node(k, new Point(i * 120 + 30, 30 + 400));
            return new Node(k, new Point((i % (quant / 2)) * 120 +30, 230 + 400));
        }
    }
}
