using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Drawing;

namespace SensorView
{
    public class IbeoLidar : SensorView.ISensor
    {
        public event GotDrawableDataDel GotDataPacket;
        public event GotDrawableDataErrDel GotBadPacket;
        
        
        private const int HEADER_SIZE = 16;
        private const int FILE_TYPE_COMPRESSED_SCAN = 15;
        private const UInt32 MAGIC_WORD = 0xAFFEC0C0;
        private const byte MAGIC_WORD1 = 0xAF;
        private const byte MAGIC_WORD2 = 0xFE;
        private const byte MAGIC_WORD3 = 0xC0;
        private const byte MAGIC_WORD4 = 0xC0;

        private const int MESSAGE_MAX_SIZE = 103808;

        private int seqNumExp=-1;
        private int ID;
        int totalPackets = 0;
        int totalPacketsResetable = 0;

        private IAsyncResult AsyncReadResult = null;

        private byte[] tempMsgBuf = new byte[MESSAGE_MAX_SIZE];
        private byte[] rxbuf = new byte[MESSAGE_MAX_SIZE];        

        private TcpClient tcpClient = null;
        private UdpClient udpClientRX = null;
				private UdpClient udpClientTX = new UdpClient();
			  private IPEndPoint ipendTX;

        private MemoryStream tempMsg;

        private bool isConnected = false;

        int syncState = 0;
        int msgSize = 0;

        public List<IbeoPacket> lidarPacketBuffer = new List<IbeoPacket>();
        //public TimeStampPacket tsPacketBuffer;
				public Queue<TimeStampPacket> tsPacketBufferList = new Queue<TimeStampPacket>();

        private void OnReadBytesComplete(IAsyncResult AsyncResult)
        {
            int bytesRead = tcpClient.GetStream().EndRead(AsyncResult);

            //THE BRIAN STATE MACHINE
            switch (syncState)
            {
                case 0:
                    if (rxbuf[0] == MAGIC_WORD1)
                    {
                        syncState++;
                        tempMsg.Position = 0;
                        tempMsg.WriteByte(rxbuf[0]);
                    }
                    else
                    {
                        if (GotBadPacket != null) GotBadPacket(this, new DrawDataPacketErrEventArgs(0, this));
                    }
                    AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 1, new AsyncCallback(OnReadBytesComplete), null);
                    break;
                case 1:
                    if (rxbuf[0] == MAGIC_WORD2)
                    {
                        syncState++;
                        tempMsg.WriteByte(rxbuf[0]);
                    }
                    else
                    {
                        syncState = 0;
												if (GotBadPacket != null) GotBadPacket(this, new DrawDataPacketErrEventArgs(0, this));
                    }
                    AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 1, new AsyncCallback(OnReadBytesComplete), null);                    
                    break;
                case 2:
                    if (rxbuf[0] == MAGIC_WORD3)
                    {
                        syncState++;
                        tempMsg.WriteByte(rxbuf[0]);
                    }
                    else
                    {
                        syncState = 0;
												if (GotBadPacket != null) GotBadPacket(this, new DrawDataPacketErrEventArgs(0, this));
                    }
                    AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 1, new AsyncCallback(OnReadBytesComplete), null);                    
                    break;
                case 3:
                    if (rxbuf[0] == MAGIC_WORD4)
                    {
                        syncState++;
                        tempMsg.WriteByte(rxbuf[0]);
                    }
                    else
                    {
                        syncState = 0;
												if (GotBadPacket != null) GotBadPacket(this, new DrawDataPacketErrEventArgs(0, this));
                    }
                    AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 12, new AsyncCallback(OnReadBytesComplete), null);                    
                    break;
                case 4:
                    tempMsg.Write(rxbuf, 0, bytesRead);
                    if (tempMsg.Position == HEADER_SIZE) //got everything
                    {
                        syncState++;
                        BinaryReader br = new BinaryReader(new MemoryStream (tempMsgBuf,0,HEADER_SIZE));
                        //check to see the header....            
                        BigToLittle(br.ReadUInt32()); //read the header
                        msgSize = Convert.ToInt32(BigToLittle(br.ReadUInt32()));
                        AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, msgSize, new AsyncCallback(OnReadBytesComplete), null);       
                    }
                    else //read the rest....
                    {
                        AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 12 - bytesRead, new AsyncCallback(OnReadBytesComplete), null);       
                    }                    
                    break;
                case 5:
                    tempMsg.Write(rxbuf, 0, bytesRead);
                    if (tempMsg.Position >= (msgSize + HEADER_SIZE)) //got everything
                    {
                        syncState=0;

                        if (tempMsg.Position > (msgSize + HEADER_SIZE))
													if (GotBadPacket != null) GotBadPacket(this, new DrawDataPacketErrEventArgs(0, this));
													if (GotDataPacket != null)
													{
														IbeoPacket p = new IbeoPacket();
														TimeStampPacket tsp;
														if (tsPacketBufferList.Count !=0 )
														 tsp = tsPacketBufferList.Peek();
														else
														{
															tsp = new TimeStampPacket ();
															tsp.CarTimeSeconds = 0;
															tsp.CarTimeTicks = 0;
															tsp.SeqNumber = 0;
														}
														totalPackets++;
														totalPacketsResetable++;
														DataPoint[] d = processIbeoPacket(ref p, tempMsg);
														if (d!=null)
														GotDataPacket(this, new DrawDataPacketEventArgs(d, this, tsp));
														if (p.DataType == FILE_TYPE_COMPRESSED_SCAN) //we got a data packet
														{
															//Console.WriteLine("SCAN");
															//if (tsPacketBuffer.SeqNumber == 0) 
															TimeStampPacket ts;
															if (tsPacketBufferList.Count>0)
																 ts = tsPacketBufferList.Dequeue();
															else
																System.Console.WriteLine("WARNING: LOST SEQ NUMBER!");															
														}
													}
												AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 1, new AsyncCallback(OnReadBytesComplete), null);
                    }
                    else
                    {
                        AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, msgSize - bytesRead, new AsyncCallback(OnReadBytesComplete), null);
                    }                  
    
                    break;
            }         
        }

        private DataPoint[] processIbeoPacket(ref IbeoPacket p, MemoryStream ms)
        {
           
            ms.Position = 0;
            
            BinaryReader br = new BinaryReader(ms);
            DataPoint[] points = null;
            p.MagicWord = BigToLittle(br.ReadUInt32());
            if (p.MagicWord != MAGIC_WORD) throw new Exception("FATAL BAD PACKET IN PROCESS!!!!");
            p.Size = BigToLittle(br.ReadUInt32());
            p.DataType = BigToLittle(br.ReadUInt32());
            p.IbeoTimeStamp = BigToLittle(br.ReadUInt32());

            if (p.DataType == FILE_TYPE_COMPRESSED_SCAN) //FILE TYPE COMPRESSED SCAN
            {			
                p.Version = br.ReadByte();
                p.ScannerType = br.ReadByte();
                p.ECUID = br.ReadByte();
                p.PAD = br.ReadByte();
                p.IbeoTimeStamp = BigToLittle(br.ReadUInt32());
                p.StartAngle = BigToLittle(br.ReadInt16());
                p.StopAngle = BigToLittle(br.ReadInt16());
                p.ScanCount = BigToLittle(br.ReadUInt16());
								//System.Console.WriteLine("Num stuffs:" + p.ScanCount);
                p.NumberOfPoints = BigToLittle(br.ReadUInt16());
                p.ScanPoints = new IbeoPoint[p.NumberOfPoints];
								points = new DataPoint[p.NumberOfPoints];
                for (int i = 0; i < p.NumberOfPoints; i++)
                {
                    p.ScanPoints[i] = new IbeoPoint();
                    p.ScanPoints[i].ScannerID = br.ReadByte();
                    p.ScanPoints[i].Channel = br.ReadByte();
                    p.ScanPoints[i].SubChannel = br.ReadByte();
                    p.ScanPoints[i].PointStatus = br.ReadByte();
                    p.ScanPoints[i].X = BigToLittle(br.ReadInt16());
                    p.ScanPoints[i].Y = BigToLittle(br.ReadInt16());
                   
                    p.ScanPoints[i].Z = BigToLittle(br.ReadInt16());
                    p.ScanPoints[i].EchoPulseWidth = BigToLittle(br.ReadUInt16());
										points[i] = new DataPoint(IbeoPointConverter(p.ScanPoints[i].X), 
                                               IbeoPointConverter(p.ScanPoints[i].Y), 
                                               IbeoPointConverter(p.ScanPoints[i].Z),
                                               p.ScanPoints[i].Channel, 
                                               p.ScanPoints[i].SubChannel,
                                               p.ScanPoints[i].PointStatus);
                }
            }
            return points;
        }

        private float IbeoPointConverter(int x)
        {
            float Xact = 0; 
            if (x < -10000)
                Xact = x * 0.1f + 900.0f;
            if ((x >= -10000) && (x <= 10000))
                Xact = x * .01f;
            if (x > 10000)
                Xact = x * 0.1f - 900.0f;
            return Xact;
        }



        public struct IbeoPacket
        {
            public UInt32 MagicWord;
            public UInt32 Size;
            public UInt32 DataType;
            public UInt32 IbeoTimeStamp;
            public UInt32 CarTimeTicks;
            public UInt16 CarTimeSecs;
            public UInt32 SequenceNum;
            public Byte Version;
            public Byte ScannerType;
            public Byte ECUID;
            public Byte PAD;
            public UInt32 TimeStampScan;
            public Int16 StartAngle;
            public Int16 StopAngle;
            public UInt16 ScanCount;
            public UInt16 NumberOfPoints;
            public IbeoPoint[] ScanPoints;
        }

        public struct IbeoPoint
        {
            public Byte ScannerID;
            public Byte Channel;
            public Byte SubChannel;
            public Byte PointStatus;
            public Int16 X;
            public Int16 Y;
            public Int16 Z;
            public UInt16 EchoPulseWidth;
        }

        #region bigtolittle
        private UInt32 BigToLittle(UInt32 stuff)
        {
            Byte[] stuffness = BitConverter.GetBytes(stuff);
            Array.Reverse(stuffness);
            return BitConverter.ToUInt32(stuffness, 0);
        }
        private UInt16 BigToLittle(UInt16 stuff)
        {
            Byte[] stuffness = BitConverter.GetBytes(stuff);
            Array.Reverse(stuffness);
            return BitConverter.ToUInt16(stuffness, 0);
        }
        private Int16 BigToLittle(Int16 stuff)
        {
            Byte[] stuffness = BitConverter.GetBytes(stuff);
            Array.Reverse(stuffness);
            return BitConverter.ToInt16(stuffness, 0);
        }
        #endregion

        private void TSReceiveCallback(IAsyncResult ar)
        {

					try
					{
						
						IPEndPoint e = new IPEndPoint(IPAddress.Any, 0);
						Byte[] receiveBytes = udpClientRX.EndReceive(ar, ref e);
						udpClientRX.BeginReceive(new AsyncCallback(TSReceiveCallback), ar.AsyncState);
						
						//we got a new timing packet, throw it on the stack

						BinaryReader br = new BinaryReader(new MemoryStream(receiveBytes));

						UInt16 seconds = BigToLittle(br.ReadUInt16());
						UInt32 ticks = BigToLittle(br.ReadUInt32());
						UInt32 seq = BigToLittle(br.ReadUInt32());
						TimeStampPacket tsp = new TimeStampPacket();
						tsp.SeqNumber = seq;
						tsp.CarTimeTicks = ticks;
						tsp.CarTimeSeconds = seconds;
						//tsPacketBuffer = tsp;
						tsPacketBufferList.Enqueue(tsp);
						//Console.WriteLine("UDP");
						if (seqNumExp == -1) //firstpacket
						{
							seqNumExp = (int)seq;
							Console.WriteLine("INIT: Seq Num! GOT: " + seq + " SET: " + seqNumExp);
						}
						else if (seqNumExp != seq)
						{
							Console.WriteLine("WARNING: Seq mismatch! GOT: " + seq + " EXP: " + seqNumExp);
							seqNumExp = (int)seq;
						}
						seqNumExp++;
					}
					catch (SocketException e)
					{
						Console.WriteLine("SOCKET EXCEPTION IN IBEO: " + e.Message);
					}
        }

        public IbeoLidar(int id ,string IbeoIP, int IbeoPort, string TSIP, int TSPort)
        {
            
            this.ID = id;
            tcpClient = new TcpClient();
            IPEndPoint stuff = new IPEndPoint(IPAddress.Any, 30009);
            udpClientRX = new UdpClient(stuff);
						udpClientRX.JoinMulticastGroup (IPAddress.Parse ("239.132.1.9"));
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            tcpClient.LingerState = new LingerOption(false, 0);
            tempMsg = new MemoryStream(tempMsgBuf);         
						ipendTX = new IPEndPoint (IPAddress.Parse(TSIP),20);
            Connect (IbeoIP,IbeoPort);
						SendRegister(TSPort);
            udpClientRX.BeginReceive(new AsyncCallback(TSReceiveCallback), this);
        }

			private void SendRegister(int port)
			{
				if  (ipendTX == null) throw new Exception ("Did not initialize TX endpoint in IBEO TS!");
				byte[] stuff = { 0x00, 0x00, 0x00, 0x00, 0x00, (byte)((port>>8)&0xff), (byte)(port&0xff)};
				udpClientTX.Send(stuff, stuff.Length, ipendTX);
		
			}
        private void Connect(string IP, int port)
        {
            try
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Parse(IP), port);
                tcpClient.Connect(ip);
                isConnected = true;                
                tcpClient.NoDelay = true;
                AsyncReadResult = tcpClient.GetStream().BeginRead(rxbuf, 0, 1, new AsyncCallback(OnReadBytesComplete), this);
            }
            catch (Exception ex)
            {
                throw new Exception ("Exception : \n" + ex.Message);
            }
        }            
        public void Disconnect()
        {
            if (tcpClient != null)
                tcpClient.Close();
            isConnected = false;
        }
        public bool IsConnected()
        {
            return isConnected;
        }
        public int GetID()
        {
            return ID;
        }
        public override string ToString()
        {
            return "IBEO Lidar " + ID + ":";
        }


        public int GetTotalNumberPackets()
        {
            return totalPackets;
        }

        public void ResetNumberPackets()
        {
            totalPacketsResetable = 0;
        }

        public int GetNumberPackets()
        {
            return totalPacketsResetable;
        }

    }

   
    
}
