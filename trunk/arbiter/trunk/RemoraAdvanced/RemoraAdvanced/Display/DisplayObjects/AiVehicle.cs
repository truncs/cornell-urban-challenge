using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using RemoraAdvanced.Common;
using RndfEditor.Display.Utilities;

namespace RemoraAdvanced.Display.DisplayObjects
{
	/// <summary>
	/// Ai Vehicle
	/// </summary>
	public class AiVehicle : CarDisplayObject
	{
		/// <summary>
		/// State of vehicle
		/// </summary>
		public VehicleState State
		{
			get
			{
				return RemoraCommon.Communicator.GetVehicleState();
			}
		}

		/// <summary>
		/// Type of selection
		/// </summary>
		private SelectionType selection = SelectionType.NotSelected;

		public override RearAxleType RearAxleType
		{
			get { return RearAxleType.Rear; }
		}

		public override UrbanChallenge.Common.Coordinates Position
		{
			get
			{
				return State.Position;
			}
			set
			{				
			}
		}

		public override UrbanChallenge.Common.Coordinates Heading
		{
			get
			{
				return State.Heading;
			}
			set
			{				
			}
		}

		public override double Width
		{
			get
			{
				return TahoeParams.T;
			}
			set
			{
				
			}
		}

		public override double Length
		{
			get
			{
				return TahoeParams.VL;
			}
			set
			{
				
			}
		}

		protected override System.Drawing.Color color
		{
			get { return DrawingUtility.ColorSimAiCar; }
		}

		protected override RndfEditor.Display.Utilities.SelectionType selectionType
		{
			get { return selection; }
		}

		protected override float steeringAngle
		{
			get { return 0; }
		}

		protected override string Id
		{
			get { return ""; }
		}

		public override bool MoveAllowed
		{
			get { return false; }
		}

		public override void BeginMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override void CancelMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override RndfEditor.Display.Utilities.SelectionType Selected
		{
			get
			{
				return selection;
			}
			set
			{
				this.selection = value;
			}
		}

		public override bool CanDelete
		{
			get { return false; }
		}

		public override List<RndfEditor.Display.Utilities.IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public override bool ShouldDraw()
		{
			return this.State != null && DrawingUtility.DrawAiVehicle && DrawingUtility.DrawSimCars;
		}
	}
}
