using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comp_networks_routing
{
    public enum PackageType { Hello, Ack, Inf, Connect, RR};
    public enum MessageType { Data, HelloPath};

    public struct Package
    {
        public uint messageId;
        public uint id;
        public uint quantity;
        public uint destination;
        public PackageType type;
        public uint size;
        public bool service;
    }

    class Message
    {
        static uint NewId;

        internal int messageSize;
        internal bool datagram;
        internal int packSize;
        internal int pCount;
        internal int servicePartSize;
        internal int MaxPackSize;
        internal uint destination;
        internal uint id;
        internal MessageType type;
        internal uint delayTime;
        internal uint creationTime = 0,
            endTime = 0;

        public uint CurrentNode { get; set; }
        public uint NextNode { get; set; }
        public List<Package> packages;

        public Message(bool mode)
        {
            Random rand = new Random();
            messageSize = rand.Next(200, 2024);
            datagram = mode;
            MaxPackSize = 300;
            packages = new List<Package>();
            id = Message.NewId++;
            FormPack();
        }

        public Message(bool mode, int mesSize, int maxSize = 2)
        {
            messageSize = mesSize;
            datagram = mode;
            MaxPackSize = maxSize;
            packages = new List<Package>();
            id = Message.NewId++;
            FormPack();
            delayTime = 0;
        }

        public Message(bool mode, int size, MessageType type, uint dest)
        {
            messageSize = size;
            datagram = mode;
            this.type = type;
            destination = dest;
            MaxPackSize = 16;
            packages = new List<Package>();
            id = Message.NewId++;
            FormPack();
        }

        void FormPack()
        {
            int remain;
            pCount = Math.DivRem(messageSize, MaxPackSize,out remain);
            if (remain > 0)
                pCount++;
            if (!datagram)
            {
                packages.Add(new Package()
                {
                    id = 0,
                    messageId = this.id,
                    type = PackageType.Connect,
                    size = 1
                });
                packages.Add(new Package()
                {
                    id = 0,
                    messageId = this.id,
                    type = PackageType.Ack,
                    size = 1
                });
            }
            for (uint i=0; i<pCount; i++)
            {
                packages.Add(new Package()
                {
                    id = i,
                    messageId = this.id,
                    destination = this.destination,
                    quantity = (uint)pCount,
                    type = PackageType.Inf,
                    service = type == MessageType.HelloPath ? true : false,
                    size = (uint) (i == pCount - 1 ? remain : MaxPackSize)
                });
                if(!datagram)
                    packages.Add(new Package()
                    {
                        id = i,
                        messageId = this.id,
                        destination = this.destination,
                        quantity = (uint)pCount,
                        type = PackageType.RR,
                        size = 1
                    });
            }
            if (!datagram)
            {
                packages.Add(new Package()
                {
                    id = 0,
                    messageId = this.id,
                    type = PackageType.Connect,
                    size = 1
                });
                packages.Add(new Package()
                {
                    id = 0,
                    messageId = this.id,
                    type = PackageType.Ack,
                    size = 1
                });
            }
        }
    }
}
