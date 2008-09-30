using System;


namespace SensorView
{
    public delegate void GotDrawableDataDel(object sender, DrawDataPacketEventArgs e);
    public delegate void GotDrawableDataErrDel(object sender, DrawDataPacketErrEventArgs e);


    public interface ISensor
    {
        event GotDrawableDataDel GotDataPacket;
        event GotDrawableDataErrDel GotBadPacket;
        bool IsConnected();
        int  GetID();
        int GetTotalNumberPackets();
        int GetNumberPackets();
        void ResetNumberPackets();
    }

    public struct TimeStampPacket
    {
        public UInt32 CarTimeTicks;
        public UInt16 CarTimeSeconds;
        public UInt32 SeqNumber;
    }

	public struct Point3
	{
		public float X; public float Y; public float Z;

		public Point3(float x, float y)
		{
			this.X = x; this.Y = y; this.Z = 0;
		}
		public Point3(float x, float y, float z)
		{
			this.X = x; this.Y = y; this.Z = z;
		}		
	}
    public struct DataPoint
    {
        public DataPoint(float x, float y, float z, int ray, int echo)
        {
						this.closestPoint = new Point3(x, y,z);
						this.ccwPoint = new Point3();
						this.cwPoint = new Point3(); 
            this.rayNumber = ray;
            this.echoNumber = echo;
            this.type = 0;            
						this.isBox = false;
        }
        public DataPoint(float x, float y, float z, int ray, int echo, int type)
        {
						this.closestPoint = new Point3(x, y, z);
						this.ccwPoint = new Point3(0, 0);
						this.cwPoint = new Point3(0, 0); 
            this.rayNumber = ray;
            this.echoNumber = echo;
            this.type = type;
            
						this.isBox = false;
        }

				public DataPoint(float range, float theta)
				{
					float y = (float)Math.Sin(theta) * (float)range;
					float x = (float)Math.Cos(theta) * (float)range;

					this.closestPoint = new Point3(x, y);
					this.ccwPoint = new Point3(0, 0);
					this.cwPoint = new Point3(0, 0);
					this.rayNumber = 0;
					this.echoNumber = 0;
					this.type = 0;					
					this.isBox = false;
				}

				public DataPoint(float range, float theta, float rangeCW, float thetaCW, float rangeCCW, float thetaCCW)
				{
					float y = (float)Math.Sin(theta) * (float)range;
					float x = (float)Math.Cos(theta) * (float)range;
					float yCW = (float)Math.Sin(thetaCW) * (float)rangeCW;
					float xCW = (float)Math.Cos(thetaCW) * (float)rangeCW;
					float yCCW = (float)Math.Sin(thetaCCW) * (float)rangeCCW;
					float xCCW = (float)Math.Cos(thetaCCW) * (float)rangeCCW;

					this.closestPoint = new Point3(x, y);
					this.cwPoint = new Point3(xCW, yCW);
					this.ccwPoint = new Point3(xCCW, yCCW);
					this.rayNumber = 0;
					this.echoNumber = 0;
					this.type = 0;					
					this.isBox = true;
				}

				public Point3 closestPoint;
				public Point3 cwPoint;
				public Point3 ccwPoint;
        
        public int rayNumber;
        public int echoNumber;
        public int type;
				public bool isBox;


    }
    public class DrawDataPacketEventArgs : EventArgs
    {
        public DrawDataPacketEventArgs(DataPoint[] data, ISensor lidar, TimeStampPacket tsp)
        {
            this.data = data;
            this.lidar = lidar;
            this.tsp = tsp;
        }
        public DataPoint[] data;
        public ISensor lidar;
        public TimeStampPacket tsp;

    }

    public class DrawDataPacketErrEventArgs : EventArgs
    {
        public DrawDataPacketErrEventArgs(int errnum, ISensor lidar)
        {
            this.errnum = errnum;
            this.lidar = lidar;
        }
        public int errnum;
        public ISensor lidar;
    }

   
}
