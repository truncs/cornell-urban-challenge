using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using UrbanChallenge.Common;
using System.Drawing;

namespace Remora.Display
{
	/// <summary>
	/// Keeps a track of the posterior pose over time
	/// </summary>
	public class PosteriorPoseTracks : IDisplayObject
	{
		public Queue<Coordinates> log;
		public int size;

		/// <summary>
		/// Constructor
		/// </summary>
		public PosteriorPoseTracks(int size)
		{
			log = new Queue<Coordinates>();
			this.size = size;
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			Coordinates[] coordArray = log.ToArray();
			Coordinates wll = t.WorldLowerLeft;
			Coordinates wur = t.WorldUpperRight;			

			if (DrawingUtility.DisplayPoseLog)
			{
				if (log.Count > 1)
				{
					for (int i = 0; i < coordArray.Length - 1; i++)
					{
						if(wll.X < coordArray[i].X && wll.Y < coordArray[i].Y && wur.X > coordArray[i].X && wur.Y > coordArray[i].Y)
							DrawingUtility.DrawControlLine(coordArray[i], coordArray[i + 1], Color.OrangeRed, g, t);
					}
				}
			}
		}

		public void Update(Coordinates coords)
		{
			if (size == 0)
			{
				log.Enqueue(coords);
			}
			else if (log.Count > size)
			{
				log.Dequeue();
				log.Enqueue(coords);
			}
			else
				log.Enqueue(coords);
		}

		public void Restart()
		{
			log = new Queue<Coordinates>();
		}

		public void Restart(int size)
		{
			log = new Queue<Coordinates>();
			this.size = size;
		}

		#endregion
	}
}
