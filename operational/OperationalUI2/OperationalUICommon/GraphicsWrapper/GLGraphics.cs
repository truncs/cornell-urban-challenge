using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using UrbanChallenge.Common.Splines;
using System.Drawing.Drawing2D;
using Tao.OpenGl;
using UrbanChallenge.Common;
using Tao.Platform.Windows;

namespace UrbanChallenge.OperationalUI.Common.GraphicsWrapper {
	public class GLGraphics : IGraphics {
		private static Bitmap measureBitmap;
		private static Graphics measureGraphics;

		static GLGraphics() {
			measureBitmap = new Bitmap(240, 120, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			measureGraphics = Graphics.FromImage(measureBitmap);
		}

		private WorldTransform transform;
		private SimpleOpenGlControl glControl;

		private Dictionary<string, int> fontCache;

		public GLGraphics(SimpleOpenGlControl control, Color clearColor) {
			this.glControl = control;
			fontCache = new Dictionary<string, int>();

			InitGL(control.Width, control.Height, clearColor, control);
			
			
		}

		public void InitGL(int width, int height, Color clearColor, SimpleOpenGlControl ctrl) {
			//Gl.glBlendFunc(Gl.GL_ONE, Gl.GL_ZERO);
			Gl.glClearColor(((float)clearColor.R / 255.0f), ((float)clearColor.G / 255.0f), ((float)clearColor.B / 255.0f), 0);
			Gl.glClearDepth(1);
			//Gl.glDepthFunc(Gl.GL_ALWAYS); //stuff the z buffer
			//Gl.glEnable(Gl.GL_DEPTH_TEST);
			Gl.glDisable(Gl.GL_DEPTH_TEST);
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glEnable(Gl.GL_LINE_STIPPLE);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_SMOOTH);
			Gl.glViewport(0, 0, width, height);
			//Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
			Gl.glLineWidth(1.5f);
		}

		#region IGraphics Members

		public IPen CreatePen() {
			return new GLPen();
		}

		public void DrawBezier(IPen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4) {
			CubicBezier cb = new CubicBezier(Utility.ToCoord(pt1), Utility.ToCoord(pt2), Utility.ToCoord(pt3), Utility.ToCoord(pt4));
			ApplyPen(pen);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			//iterate this bitch
			for (double i = 0; i < 1.0; i += .025) {
				PointF p = Utility.ToPointF(cb.Bt(i));
				Gl.glVertex2f(p.X, p.Y);
			}

			PointF p1 = Utility.ToPointF(cb.Bt(1));
			Gl.glVertex2f(p1.X, p1.Y);

			Gl.glEnd();			
		}

		public void DrawBeziers(IPen pen, PointF[] pts) {
			throw new NotSupportedException();
		}

		public void DrawEllipse(IPen pen, RectangleF rect) {
			ApplyPen(pen);
			Gl.glPushMatrix();
			Gl.glTranslatef(rect.Left+rect.Width /2, rect.Top + rect.Height /2, 0);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			for (double i = 0; i < Math.PI * 2; i += 0.05)
				Gl.glVertex2f((float)(Math.Cos(i) * rect.Width/2), (float)(Math.Sin(i) * rect.Height/2));
			Gl.glEnd();
			Gl.glPopMatrix();
		}

		public void DrawRectangle(IPen pen, RectangleF rect) {
			ApplyPen(pen);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			Gl.glVertex2f(rect.Left, rect.Top);
			Gl.glVertex2f(rect.Right, rect.Top);
			Gl.glVertex2f(rect.Right, rect.Bottom);
			Gl.glVertex2f(rect.Left, rect.Bottom);
			Gl.glEnd();
		}

		public void DrawLine(IPen pen, PointF pt1, PointF pt2) {
			ApplyPen(pen);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			Gl.glVertex2f(pt1.X, pt1.Y);
			Gl.glVertex2f(pt2.X, pt2.Y);
			Gl.glEnd();
		}

		public void DrawLines(IPen pen, PointF[] pts) {
			ApplyPen(pen);
			Gl.glBegin(Gl.GL_LINE_STRIP);
			foreach (PointF p in pts) {
				Gl.glVertex2f(p.X, p.Y);
			}
			Gl.glEnd();
		}

		public void DrawPolygon(IPen pen, PointF[] pts) {
			ApplyPen(pen);
			Gl.glBegin(Gl.GL_LINE_LOOP);
			foreach (PointF p in pts) {
				Gl.glVertex2f(p.X, p.Y);
			}
			Gl.glEnd();
		}

		public void DrawCross(IPen pen, PointF p, float size) {
			PointF p1 =  new PointF(p.X, p.Y + size/2.0f);
			PointF p2 =  new PointF(p.X, p.Y - size/2.0f);
			PointF p3 =  new PointF(p.X + size/2.0f, p.Y);
			PointF p4 =  new PointF(p.X - size/2.0f, p.Y);
			DrawLine(pen, p1, p2);
			DrawLine(pen, p3, p4);
		}

		public void FillRectangle(Color color, RectangleF rect) {
			SetGLColor(color);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(rect.Left, rect.Top);
			Gl.glVertex2f(rect.Right, rect.Top);
			Gl.glVertex2f(rect.Right, rect.Bottom);
			Gl.glVertex2f(rect.Left, rect.Bottom);
			Gl.glVertex2f(rect.Left, rect.Top);
			Gl.glEnd();				
		}

		public void FillPolygon(Color color, PointF[] pts) {
			SetGLColor(color);
			Gl.glBegin(Gl.GL_POLYGON);
			for (int i = 0; i < pts.Length; i++) {
				Gl.glVertex2f(pts[i].X, pts[i].Y);
			}
			//Gl.glVertex2f(pts[0].X, pts[0].Y);
			Gl.glEnd();
		}

		public void FillEllipse(Color color, RectangleF rect) {
			SetGLColor(color);
			Gl.glPushMatrix();
			Gl.glTranslatef(rect.Location.X + rect.Width / 2, rect.Location.Y + rect.Height / 2, 0);
			Gl.glBegin(Gl.GL_POLYGON);
			for (double i=0; i < Math.PI * 2; i+= 0.05)
				Gl.glVertex2f((float)(Math.Cos(i)*rect.Width/2), (float)(Math.Sin(i)*rect.Height/2));
			Gl.glEnd();
			Gl.glPopMatrix();
		}

		public SizeF MeasureString(string str, Font font) {
			return measureGraphics.MeasureString(str, font);
		}

		public void DrawString(string str, Font font, Color color, PointF loc) {
			int fontbase = GetCachedFontBase(font);

			SetGLColor(color);
			Gl.glRasterPos2f(loc.X, loc.Y);

			if (string.IsNullOrEmpty(str)) 
				return;

			byte[] textbytes = Encoding.ASCII.GetBytes(str);

			Gl.glPushAttrib(Gl.GL_LIST_BIT);
			Gl.glListBase(fontbase - 32);
			Gl.glCallLists(textbytes.Length, Gl.GL_UNSIGNED_BYTE, textbytes);
			Gl.glPopAttrib();
		}

		public void InitScene(WorldTransform wt, Color background) {
			this.transform = wt;
			Coordinates ll = transform.WorldLowerLeft;
			Coordinates ur = transform.WorldUpperRight;

			Gl.glViewport(0, 0, (int)transform.ScreenSize.Width, (int)transform.ScreenSize.Height);
			Gl.glLoadIdentity();
			Gl.glOrtho(ll.X, ur.X, ll.Y, ur.Y, 1.0, -1.0);

			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
		}

		public void GoToVehicleCoordinates(PointF pos, float heading) {
			Gl.glPushMatrix(); //put the current matrix on the stack
			Gl.glTranslatef(pos.X, pos.Y, 0);
			Gl.glRotatef((heading * 180 / (float)Math.PI - 90), 0.0f, 0.0f, 1.0f);
		}

		public void ComeBackFromVehicleCoordinates() {
			Gl.glPopMatrix();
		}

		public void PushMatrix() {
			Gl.glPushMatrix();
		}

		public void PopMatrix() {
			Gl.glPopMatrix();
		}

		public void Translate(float dx, float dy) {
			Gl.glTranslatef(dx, dy, 0);
		}

		public void Rotate(float theta) {
			Gl.glRotatef(theta, 0, 0, 1.0f);
		}

		#endregion

		private void ApplyPen(IPen pen) {
			float width = pen.Width*transform.Scale;

			if (width < 1.0f) width = 1.0f;
			Gl.glColor4f(pen.Color.R/255.0f, pen.Color.G/255.0f, pen.Color.B/255.0f, pen.Color.A/255.0f);
			Gl.glLineWidth(width*1.5f);
			switch (pen.DashStyle) {
				case DashStyle.Dash:
					Gl.glLineStipple(0, (short)0x00FF);
					break;

				case DashStyle.Dot:
					Gl.glLineStipple(0, (short)0x3333);
					break;

				case DashStyle.DashDot:
				case DashStyle.DashDotDot:
					Gl.glLineStipple(0, (short)0x7F33);
					break;

				case DashStyle.Solid:
				default:
					Gl.glLineStipple(0, -1);
					break;
			}
		}

		private void SetGLColor(Color color) {
			Gl.glColor4ub(color.R, color.G, color.B, color.A);
		}

		private int GetCachedFontBase(Font f) {
			string fontString = GetFontString(f);

			int fontbase;
			if (!fontCache.TryGetValue(fontString, out fontbase)) {
				fontbase = BuildFont(f);
				fontCache.Add(fontString, fontbase);
			}

			return fontbase;
		}

		private int BuildFont(Font f) {
			int fontbase = Gl.glGenLists(96);

			// get the device context
			IntPtr hDC = User.GetDC(glControl.Handle);

			IntPtr hFont = f.ToHfont();
			IntPtr oldfont = Gdi.SelectObject(hDC, hFont);						// Selects The Font We Want
			Wgl.wglUseFontBitmaps(hDC, 32, 96, fontbase);             // Builds 96 Characters Starting At Character 32
			Gdi.SelectObject(hDC, oldfont);                           // swap in the old font
			Gdi.DeleteObject(hFont);                                  // Delete the font handle we obtained
			User.ReleaseDC(glControl.Handle, hDC);										// release the hDC we acquired

			// return the font base
			return fontbase;
		}

		private string GetFontString(Font f) {
			return f.Name + "," + f.SizeInPoints.ToString() + "," + f.Style.ToString();
		}

	}
}
