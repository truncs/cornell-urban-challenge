using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Operational.Common;
using System.Reflection;

namespace UrbanChallenge.OperationalUI.Controls {
	public partial class ArcVotingDisplay : UserControl {
		private ArcVotingResults results = null;
		private FieldInfo selectedField = null;

		public ArcVotingDisplay() {
			InitializeComponent();

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);

			GetDataSources();
		}

		public void SetArcVotingResults(ArcVotingResults results) {
			this.results = results;
			this.Invalidate();
		}

		public string[] GetDataSources() {
			FieldInfo[] fields = typeof(ArcResults).GetFields();

			List<string> dataSources = new List<string>();
			foreach (FieldInfo field in fields) {
				if (field.FieldType == typeof(double) && field.Name.EndsWith("Utility")) {
					dataSources.Add(field.Name);

					if (field.Name == "totalUtility")
						selectedField = field;
				}
			}

			return dataSources.ToArray();
		}

		public string DataSource {
			get {
				if (selectedField != null) {
					return selectedField.Name;
				}
				else {
					return null;
				}
			}
			set {
				SetDataSource(value);
			}
		}

		public void SetDataSource(string fieldName) {
			FieldInfo field = typeof(ArcResults).GetField(fieldName);
			if (field != null)
				selectedField = field;
		}

		public double[] GetValues(FieldInfo field) {
			double[] values = new double[results.arcResults.Count];
			for (int i = 0; i < results.arcResults.Count; i++) {
				values[i] = (double)field.GetValue(results.arcResults[i]);
			}

			return values;
		}

		protected override void OnPaint(PaintEventArgs e) {
			Graphics g = e.Graphics;
			g.ResetClip();
			g.Clear(Color.Black);

			ArcVotingResults results = this.results;

			int maxHeight = this.ClientSize.Height - 20;
			int halfHeight = maxHeight/2;
			int centerHeight = this.ClientRectangle.Top + halfHeight + 10;
			int totalWidth = this.ClientSize.Width - 20;
			int startX = this.ClientRectangle.Left + 10;

			if (results != null && selectedField != null) {
				results.arcResults.Reverse();
				int barWidth = totalWidth/results.arcResults.Count;

				double[] values = GetValues(selectedField);
				for (int i = 0; i < values.Length; i++) {
					int height = (int)Math.Round(halfHeight*values[i]);
					int left = startX + barWidth*i;

					if (height == 0)
						continue;

					Rectangle rect;
					if (height > 0) {
						rect = new Rectangle(left, centerHeight-height, barWidth, height);
					}
					else {
						rect = new Rectangle(left, centerHeight, barWidth, -height);
					}

					Color color = Color.Blue;
					if (results.arcResults[i].vetoed) {
						color = Color.Red;
					}
					else if (results.selectedArc != null && results.selectedArc.curvature == results.arcResults[i].curvature) {
						color = Color.Green;
					}

					using (SolidBrush b = new SolidBrush(color)) {
						g.FillRectangle(b, rect);
					}
				}
			}

			using (Pen p = new Pen(Color.White, 1)) {
				g.DrawLine(p, startX, centerHeight, this.ClientSize.Width - 10, centerHeight);
			}
		}
	}
}