using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Comp_networks_routing
{
    class Node
    {
        static uint nodeCounter = 1;
        internal uint netId;
        internal uint id;
        internal Point coordinates;
        internal int bufferSize;
        internal int occupied;
        internal bool sending;
        internal bool receiving;
        internal bool enabled = true;
        internal Dictionary<uint, Message> messages;

        //Statistics
        internal uint ReceivedKB;
        internal uint ReceivedPackages;
        internal uint ReceivedServiceKB;
        internal uint ReceivedServicePackages;

        private uint ReceivedServiceB;

        public Node(uint NId, Point point, int buf = 400)
        {
            id = nodeCounter;
            nodeCounter++;
            netId = NId;
            bufferSize = buf;
            coordinates = new Point(point.X, point.Y);
            occupied = 0;
            sending = false;
            receiving = false;
            messages = new Dictionary<uint, Message>();
        }

        public bool StoreMessage(Message m)
        {
            if (messages.ContainsKey(m.id)) return false;
            if (m.messageSize > bufferSize - occupied) return false;
            messages[m.id] = m;
            occupied += m.messageSize;
            return true;
        }

        public bool RestoreMessage(Message m)
        {
            if (messages.ContainsKey(m.id))
            {
                messages.Remove(m.id);
                occupied -= m.messageSize;
                return true;
            }
            return false;
        }

        static public void RestartCounter()
        {
            nodeCounter = 1;
        }

        public void TurnOn() { enabled = true; }
        public void TurnOff() { enabled = false; }
        public bool isEnabled() { return enabled; }

        public void Receive(Package p)
        {
            switch (p.type)
            {
                case PackageType.Inf:
                    if (p.service)
                    {
                        ReceivedServiceKB += p.size;
                        ReceivedServicePackages++;
                    }
                    else
                    {
                        ReceivedKB += p.size;
                        ReceivedPackages++;
                        ReceivedServiceB += 100;
                        if (ReceivedServiceB >= 1000)
                        {
                            ReceivedServiceB = 0;
                            ReceivedServiceKB++;
                        }
                    }
                    break;
                case PackageType.Ack:
                case PackageType.Connect:
                case PackageType.Hello:
                case PackageType.RR:
                    ReceivedServiceKB += p.size;
                    ReceivedServicePackages++;
                    break;
            }
        }

        public void ClearStat()
        {
            ReceivedKB = 0;
            ReceivedPackages = 0;
            ReceivedServiceKB = 0;
            ReceivedServicePackages = 0;
        }

        override
        public string ToString()
        {
            String stat = String.Format("\n\n Received Inf KB: {0}\n Received service KB: {1}\n Receibed Inf packages: {2}\n Received service packages: {3}",
                ReceivedKB, ReceivedServiceKB*0.3, ReceivedPackages, ReceivedServicePackages);
            switch (netId)
            {
                case 5:
                    return String.Format("Information about point:\n ID: {0}\n Type: Gateway \n Buffer Size: {1} KB\n Occupied: {2}KB", id, bufferSize, occupied) + stat;
                case 6:
                    return String.Format("Information about point:\n ID: {0}\n Type: Satellite \n Buffer Size: {1} KB\n Occupied: {2}", id, bufferSize, occupied) + stat;
                default:
                    return String.Format("Information about point:\n ID: {0}\n NetID: {1}\n Buffer Size: {2} KB\n Occupied: {3}KB", id, netId, bufferSize, occupied) + stat;
            }
        }
    }
}
