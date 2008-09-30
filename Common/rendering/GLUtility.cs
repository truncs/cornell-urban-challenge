using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Tao.OpenGl;
using UrbanChallenge.Common.Splines;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using Tao.Platform.Windows;
using UrbanChallenge.Common.Path;
using System.IO;
using System.Drawing.Imaging;
using UrbanChallenge.Common.Vehicle;

namespace Rendering
{
	public static class GLUtility
	{
    
    public abstract class GLCamera
    {
			public float scale = 1.0f;
      public abstract void ApplyProjection();
      public void ApplyProjection(GLCamera prevProj, double t)
      {
        if (t <= 0.0) prevProj.ApplyProjection();
        else if (t >= 1.0)
        {
          this.ApplyProjection();
          AdjustEffects();
        }
        else
        {
          double[] p = prevProj.GetProjMatrix();
          double[] c = this.GetProjMatrix();
          for (uint i = 0; i < 16; i++)
            c[i] = c[i] * t + p[i] * (1.0 - t);
          Gl.glMultMatrixd(c);
					AdjustEffects();
        }  
      }
      public abstract void AdjustEffects();
      public double[] GetProjMatrix()
      {
        Gl.glMatrixMode(Gl.GL_PROJECTION);
        Gl.glPushMatrix();

        ApplyProjection();

        double[] ret = new double[16];
        Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, ret);
        Gl.glPopMatrix();
        return ret;
      }
      public abstract void UpdateWithWorldTransform(WorldTransform wt);
    };

		public class GLCameraChase : GLCamera
		{
			v3f eyeRel = new v3f(-10, 0, 5);
			v3f centerRel = new v3f(10, 0, 1);


			v3f eye, center, up;
      double aspect;			
      public override void AdjustEffects(){
        Gl.glEnable(Gl.GL_LIGHTING);
        //Gl.glEnable(Gl.GL_FOG);
      }
			public GLCameraChase(WorldTransform wt)
      {
				eye = eyeRel;
        center = centerRel;
        up = new v3f(0, 0, 1);
        UpdateWithWorldTransform(wt);
      }
      public override void UpdateWithWorldTransform(WorldTransform wt)
      {
        aspect = (double)(wt.ScreenSize.Width / wt.ScreenSize.Height);
				scale = wt.Scale;
      }
      public override void ApplyProjection()
      {
        Glu.gluPerspective(45.0, aspect, .01, 1000);
        Glu.gluLookAt(eye.x, eye.y, eye.z, center.x, center.y, center.z, up.x, up.y, up.z);
        
        //v3f v = (center - eye).cross(up).norm();
        //Gl.glRotatef(pitch, v.x, v.y, v.z);
      }

      public void MoveToPointAndHeading(PointF point, double rotRadians)
      {

				v3f vPoint = new v3f(point.X, point.Y, 1.0f);
				v3f forward = new v3f((float)Math.Cos(rotRadians), (float)Math.Sin(rotRadians), 0);

				float cDist = (float)Math.Sqrt(centerRel.x * centerRel.x + centerRel.y * centerRel.y);
				float cZ = centerRel.z;

				float eDist = (float)Math.Sqrt(eyeRel.x * eyeRel.x + eyeRel.y * eyeRel.y);
				float eZ = eyeRel.z;

				center = vPoint + forward * cDist;
				center.z = cZ;
				eye = vPoint - forward * eDist;
				eye.z = eZ;
      }

		
      public void Yaw(double rotDegrees)
      {
       // center = center.rotateAbout(up, (float)(rotDegrees / 180.0 * Math.PI));
      }

      public void Pitch(float rotDegrees)
      {
				v3f v = up.cross(centerRel);
				v3f cv = centerRel - eyeRel;
				centerRel = cv.rotateAbout(v, rotDegrees / 180.0f * (float)Math.PI);
      }
		}

    public class GLCameraFree : GLCamera
    {
      v3f location, up;
      float pitch, yaw; // both in degrees
      double aspect;			

      public override void AdjustEffects(){
        Gl.glEnable(Gl.GL_LIGHTING);
        //Gl.glEnable(Gl.GL_FOG);
      }
      public GLCameraFree(WorldTransform wt)
      {
        location = new v3f(-5.0f, -5, 5);
        up = new v3f(0, 0, 1);
        pitch = 30;
        yaw = 30;
        UpdateWithWorldTransform(wt);
      }
      public override void UpdateWithWorldTransform(WorldTransform wt)
      {
        aspect = (double)(wt.ScreenSize.Width / wt.ScreenSize.Height);
				scale = wt.Scale;
      }
      public override void ApplyProjection()
      {
        v3f lookat = new v3f();
        lookat.z = -(float)Math.Sin(pitch / 180.0 * Math.PI);
        float horiz = (float)Math.Cos(pitch / 180.0 * Math.PI);
        lookat.x = horiz*(float)Math.Sin(yaw / 180.0 * Math.PI);
        lookat.y = horiz*(float)Math.Cos(yaw / 180.0 * Math.PI);

        lookat = lookat + location;
        
        Glu.gluPerspective(45.0, aspect, .01, 1000);
        Glu.gluLookAt(location.x, location.y, location.z, lookat.x, lookat.y, lookat.z, up.x, up.y, up.z);
      }

      public void Pan(v3f v)
      {
        v3f lookat = new v3f();
        lookat.z = -(float)Math.Sin(pitch / 180.0 * Math.PI);
        float horiz = (float)Math.Cos(pitch / 180.0 * Math.PI);
        lookat.x = horiz * (float)Math.Sin(yaw / 180.0 * Math.PI);
        lookat.y = horiz * (float)Math.Cos(yaw / 180.0 * Math.PI);

        v3f forward = lookat;
        v3f left = up.cross(forward).norm();
        v3f T = (forward * v.x + left*v.y + up.norm()*v.z);
        
        location = location + T;
      }

      public void Yaw(float rotDegrees)
      {
        yaw += rotDegrees;
      }

      public void Pitch(float rotDegrees)
      {
        pitch += rotDegrees;
      }
    };

    public class GLCameraOrtho : GLCamera
    {
      private double left, right, bottom, top;
      private double zFar, zNear;			
      public GLCameraOrtho(WorldTransform wt)
      {
        UpdateWithWorldTransform(wt);
      }
      public override void AdjustEffects()
      {
        Gl.glDisable(Gl.GL_LIGHTING);
        Gl.glDisable(Gl.GL_FOG);
      }
      public override void UpdateWithWorldTransform(WorldTransform wt)
      {
        left = wt.WorldLowerLeft.X;
        right = wt.WorldUpperRight.X;
        bottom = wt.WorldLowerLeft.Y;
        top = wt.WorldUpperRight.Y;
        zNear = -200;
        zFar = 200;
				scale = wt.Scale;
      }
      public override void ApplyProjection()
      {
        Gl.glOrtho(left, right, bottom, top, zNear, zFar);
      }
    }

		public enum DrawRef
		{
			RearAxle,
			Center,
			BottomCorner
		}
		private static IntPtr hDC;
		private static int fontbase;
		private static Graphics measureGraphics;
		public static Font defFault = new Font("vedana", 10.0f, FontStyle.Bold);
		private static int curTextID = 0;
		private static readonly int maxNumTextures = 1;
		private static int[] textures = new int[maxNumTextures];

	
		#region Primatives
		
		public static void DrawBezier(GLPen pen, PointF startP, PointF ctrl1, PointF ctrl2, PointF endP)
		{
			CubicBezier cb = new CubicBezier(Utility.ToCoord(startP), Utility.ToCoord(ctrl1), Utility.ToCoord(ctrl2), Utility.ToCoord(endP));
			pen.GLApplyPen();
			Gl.glBegin(Gl.GL_LINE_STRIP);			
			//iterate this bitch
			for (double i = 0; i < 1.0; i += .025)
			{
				PointF p = Utility.ToPointF(cb.Bt(i));				
				Gl.glVertex2f(p.X, p.Y);	
			}

			PointF p1 = Utility.ToPointF(cb.Bt(1));
			Gl.glVertex2f(p1.X, p1.Y);	

			Gl.glEnd();			
		}
		
		public static void DrawRectangle(GLPen pen, RectangleF rect)
		{
			PointF p1 = new PointF (rect.Left, rect.Top);
			PointF p2 = new PointF (rect.Right, rect.Top);
			PointF p3 = new PointF(rect.Right, rect.Bottom);
			PointF p4 = new PointF(rect.Left, rect.Bottom);
			pen.GLApplyPen();
			Gl.glBegin(Gl.GL_LINE_LOOP);
				Gl.glVertex2f(p1.X, p1.Y);
				Gl.glVertex2f(p2.X, p2.Y);
				Gl.glVertex2f(p3.X, p3.Y);
				Gl.glVertex2f(p4.X, p4.Y);
			Gl.glEnd();
		}

    public static void DrawLine3D(GLPen pen, v3f a, v3f b)
    {
      pen.GLApplyPen();
      Gl.glBegin(Gl.GL_LINES);
      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(b.x, b.y, b.z);
      Gl.glEnd();
    }

    public static void DrawLine3D(GLPen pen, float aX, float aY, float aZ, float bX, float bY, float bZ)
    {
      pen.GLApplyPen();
      Gl.glBegin(Gl.GL_LINES);
      Gl.glVertex3f(aX, aY, aZ);
      Gl.glVertex3f(bX, bY, bZ);
      Gl.glEnd();
    }

		public static void DrawLine(GLPen pen, float x1, float y1, float x2, float y2)
		{
			DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
		}

		public static void DrawLine(GLPen pen, PointF p1, PointF p2)
		{
			pen.GLApplyPen();
			Gl.glBegin(Gl.GL_LINE_STRIP);			
			Gl.glVertex2f(p1.X, p1.Y);
			Gl.glVertex2f(p2.X, p2.Y);			
			Gl.glEnd();
		}

		public static void DrawPoint(GLPen pen, PointF p)
		{
			DrawPoint(pen, p, true);
		}

    public static void DrawPoint3D(GLPen pen, v3f p)
    {
      pen.GLApplyPen();
      Gl.glBegin(Gl.GL_POINT);
      //Gl.glVertex3f(p.x, p.y, 0.0f);//p.z);
      Gl.glVertex2f(p.x, p.y);
      Gl.glEnd();
    }

    public static void DrawWireframeBox3D_glVertexOnly(v3f a, v3f b)
    {
      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, b.y, b.z);

      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(b.x, b.y, b.z);

      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(b.x, b.y, b.z);
    }

    public static void DrawWireframeBox3D(v3f a, v3f b, GLPen pen)
    {
      pen.GLApplyPen();
      Gl.glBegin(Gl.GL_LINES);
      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, b.y, b.z);

      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(b.x, b.y, b.z);

      Gl.glVertex3f(a.x, a.y, a.z);
      Gl.glVertex3f(a.x, a.y, b.z);
      Gl.glVertex3f(b.x, a.y, a.z);
      Gl.glVertex3f(b.x, a.y, b.z);
      Gl.glVertex3f(a.x, b.y, a.z);
      Gl.glVertex3f(a.x, b.y, b.z);
      Gl.glVertex3f(b.x, b.y, a.z);
      Gl.glVertex3f(b.x, b.y, b.z);

      Gl.glEnd();
    }
    public static void DrawCube(GLPen pen, v3f p, float size)
    {
      pen.GLApplyPen();
      Gl.glPushMatrix();
      Gl.glTranslatef(p.x, p.y, p.z);
      Gl.glScalef(size, size, size);
      Gl.glBegin(Gl.GL_QUADS);
      // Front Face
      Gl.glNormal3f(0.0f, 0.0f, 1.0f);
      Gl.glVertex3f(-1.0f, -1.0f, 1.0f);
      Gl.glVertex3f(1.0f, -1.0f, 1.0f);
      Gl.glVertex3f(1.0f, 1.0f, 1.0f);
       Gl.glVertex3f(-1.0f, 1.0f, 1.0f);
      // Back Face
      Gl.glNormal3f(0.0f, 0.0f, -1.0f);
      Gl.glVertex3f(-1.0f, -1.0f, -1.0f);
      Gl.glVertex3f(-1.0f, 1.0f, -1.0f);
      Gl.glVertex3f(1.0f, 1.0f, -1.0f);
      Gl.glVertex3f(1.0f, -1.0f, -1.0f);
      // Top Face
      Gl.glNormal3f(0.0f, 1.0f, 0.0f);
      Gl.glVertex3f(-1.0f, 1.0f, -1.0f);
      Gl.glVertex3f(-1.0f, 1.0f, 1.0f);
      Gl.glVertex3f(1.0f, 1.0f, 1.0f);
      Gl.glVertex3f(1.0f, 1.0f, -1.0f);
      // Bottom Face
      Gl.glNormal3f(0.0f, -1.0f, 0.0f);
      Gl.glVertex3f(-1.0f, -1.0f, -1.0f);
      Gl.glVertex3f(1.0f, -1.0f, -1.0f);
      Gl.glVertex3f(1.0f, -1.0f, 1.0f);
      Gl.glVertex3f(-1.0f, -1.0f, 1.0f);
      // Right face
      Gl.glNormal3f(1.0f, 0.0f, 0.0f);
      Gl.glVertex3f(1.0f, -1.0f, -1.0f);
      Gl.glVertex3f(1.0f, 1.0f, -1.0f);
      Gl.glVertex3f(1.0f, 1.0f, 1.0f);
      Gl.glVertex3f(1.0f, -1.0f, 1.0f);
      // Left Face
      Gl.glNormal3f(-1.0f, 0.0f, 0.0f);
      Gl.glVertex3f(-1.0f, -1.0f, -1.0f);
      Gl.glVertex3f(-1.0f, -1.0f, 1.0f);
      Gl.glVertex3f(-1.0f, 1.0f, 1.0f);
      Gl.glVertex3f(-1.0f, 1.0f, -1.0f);
      Gl.glEnd();
      Gl.glPopMatrix();

    }

    public static void DrawCross3D(GLPen pen, v3f p, float size)
    {
      pen.GLApplyPen();
      Gl.glBegin(Gl.GL_LINES);
      Gl.glVertex3f(p.x + size, p.y, p.z);
      Gl.glVertex3f(p.x - size, p.y, p.z);
      Gl.glVertex3f(p.x, p.y + size, p.z);
      Gl.glVertex3f(p.x, p.y - size, p.z);
      Gl.glVertex3f(p.x, p.y, p.z + size);
      Gl.glVertex3f(p.x, p.y, p.z - size);
      Gl.glEnd();
    }

		public static void DrawPoint(GLPen pen, PointF p, bool ApplyPen)
		{
			if (ApplyPen) pen.GLApplyPen();
			Gl.glBegin(Gl.GL_POINT);
			Gl.glVertex2f(p.X, p.Y);
			Gl.glEnd();
		}


		public static void DrawCross(GLPen pen, PointF p, float size)
		{
			PointF p1 =  new PointF (p.X, p.Y + size/2.0f);
			PointF p2 =  new PointF (p.X, p.Y - size/2.0f);
			PointF p3 =  new PointF (p.X + size/2.0f, p.Y);
			PointF p4 =  new PointF (p.X - size/2.0f, p.Y);
			DrawLine(pen, p1, p2);
			DrawLine(pen, p3, p4);
		}

    public static void DrawDiamond(GLPen pen, PointF p, float size)
    {
      PointF p1 = new PointF(p.X, p.Y + size / 2.0f);
      PointF p2 = new PointF(p.X, p.Y - size / 2.0f);
      PointF p3 = new PointF(p.X + size / 2.0f, p.Y);
      PointF p4 = new PointF(p.X - size / 2.0f, p.Y);
      DrawLine(pen, p1, p3);
      DrawLine(pen, p3, p2);
      DrawLine(pen, p2, p4);
      DrawLine(pen, p4, p1);
    }

		public static void DrawLines(GLPen pen, PointF[] points)
		{
			pen.GLApplyPen(); 
			Gl.glBegin(Gl.GL_LINE_STRIP);			
			foreach (PointF p in points)		
				Gl.glVertex2f(p.X, p.Y);			
			Gl.glEnd();
		}

		public static void DrawCircle(GLPen p, PointF center, float radius)
		{
			p.GLApplyPen();
			Gl.glPushMatrix();
			Gl.glTranslatef(center.X, center.Y, 0);
			GLUtility.DrawEllipse(p, new RectangleF(-radius, -radius, radius * 2, radius*2));
			Gl.glPopMatrix();
		}

		public static void DrawEllipse(GLPen p, float x, float y, float width, float height)
		{
			DrawEllipse(p, new RectangleF(x, y, width, height));
		}
		public static void DrawEllipse(GLPen p, RectangleF rect)
		{
			p.GLApplyPen();
			Gl.glPushMatrix();
			Gl.glTranslatef(rect.Left+rect.Width /2, rect.Top + rect.Height /2 , 0);
			Gl.glBegin(Gl.GL_LINE_LOOP);			
			for (double i = 0; i < Math.PI * 2; i += 0.05)
				Gl.glVertex2f((float)(Math.Cos(i) * rect.Width/2), (float)(Math.Sin(i) * rect.Height/2));
			Gl.glEnd();
			Gl.glPopMatrix();
		}
		#endregion

		private static void InitTexture(int id)
		{
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, textures[id]);
		}

		public static void FillRectangle(Brush b, RectangleF rect)
		{
			FillRectangle(Color.White, rect);
		}

		public static void FillRectangle(SolidBrush b, RectangleF rect)
		{
			FillRectangle(b.Color, rect);
		}
		public static void FillRectangle(Color color, RectangleF rect)
		{
			SetGLColor(color); 
			Gl.glBegin(Gl.GL_POLYGON);				
			Gl.glVertex2f(rect.Left,rect.Top);
			Gl.glVertex2f(rect.Right, rect.Top);
			Gl.glVertex2f(rect.Right, rect.Bottom);
			Gl.glVertex2f(rect.Left, rect.Bottom);
			Gl.glVertex2f(rect.Left, rect.Top);
			Gl.glEnd();				
		}

		public static void FillTriangle(Color color, PointF p1, PointF p2, PointF p3)
		{
			SetGLColor(color);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(p1.X, p1.Y);
			Gl.glVertex2f(p2.X, p2.Y);
			Gl.glVertex2f(p3.X, p3.Y);
			Gl.glVertex2f(p1.X, p1.Y);
			Gl.glEnd();				
		}
		public static void FillTriangle(Color color, float alpha, PointF p1, PointF p2, PointF p3)
		{
			SetGLColor(color,alpha);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(p1.X, p1.Y);
			Gl.glVertex2f(p2.X, p2.Y);
			Gl.glVertex2f(p3.X, p3.Y);
			Gl.glVertex2f(p1.X, p1.Y);
			Gl.glEnd();				
		}

		public static void FillCone(Color color, float alpha, PointF p, float r, float thetaStart, float thetaEnd)
		{
		  SetGLColor(color, alpha);
			Gl.glPushMatrix();
			Gl.glTranslatef(p.X,p.Y,0.0f);
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2d(0, 0);
			Gl.glVertex2f((float)(Math.Cos(thetaStart) * r), (float)(Math.Sin(thetaStart) * r));
			for (double i = thetaStart; i < thetaEnd; i += 0.1)
				Gl.glVertex2f((float)(Math.Cos(i) * r), (float)(Math.Sin(i) * r));
			Gl.glVertex2f((float)(Math.Cos(thetaEnd) * r), (float)(Math.Sin(thetaEnd) * r));
			Gl.glVertex2d(0, 0);
			Gl.glEnd();
			Gl.glPopMatrix();
		}

		public static void FillTexturedRectangle(int textureID, RectangleF rect)
		{
			if (textureID == -1)
			{
				Console.WriteLine("Bad Texture! Bailing....");
				return;
			}
			SetGLColor(Color.White);
			Gl.glEnable(Gl.GL_TEXTURE_2D);
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);
			Gl.glBegin(Gl.GL_QUADS);
			Gl.glTexCoord2d(0.0, 1.0); Gl.glVertex2d(rect.Left, rect.Bottom);
			Gl.glTexCoord2d(1.0, 1.0); Gl.glVertex2d(rect.Right, rect.Bottom);
			Gl.glTexCoord2d(1.0, 0.0); Gl.glVertex2d(rect.Right, rect.Top );
			Gl.glTexCoord2d(0.0, 0.0); Gl.glVertex2d(rect.Left, rect.Top);
			Gl.glEnd();
			Gl.glDisable(Gl.GL_TEXTURE_2D);
		}
		public static void FillEllipse(SolidBrush b, RectangleF rect)
		{ FillEllipse(b.Color, rect); }
		public static void FillEllipse(Brush b, RectangleF rect)
		{ FillEllipse(Color.White, rect); }
		public static void FillEllipse(Color color, RectangleF rect)
		{
			SetGLColor(color);			
			Gl.glPushMatrix();
			Gl.glTranslatef(rect.Location.X + rect.Width / 2, rect.Location.Y + rect.Height / 2, 0);
			Gl.glBegin(Gl.GL_POLYGON);			
			for (double i=0; i < Math.PI * 2; i+= 0.05)			
				Gl.glVertex2f((float)(Math.Cos(i)*rect.Width/2) ,(float)(Math.Sin(i)*rect.Height/2));
			Gl.glEnd();
			Gl.glPopMatrix();

		}

		/// <summary>
		/// No longer as ghetto, but still ghetto
		/// </summary>
		/// <param name="str"></param>
		/// <param name="f"></param>
		/// <returns></returns>
		public static SizeF MeasureString(String str, Font f)
		{
			if (measureGraphics == null) {
				measureGraphics = Graphics.FromHdc(hDC);
			}
			return measureGraphics.MeasureString(str, f);
		}

		public static void DrawString(string str, Font font, SolidBrush b, PointF loc) { DrawString(str, font, b.Color, loc); }

		/// <summary>
		/// Draws a string at the specified position, note that the FONT is ignored and set to verdana, bold size 10
		/// </summary>
		/// <param name="str"></param>
		/// <param name="font"></param>
		/// <param name="color"></param>
		/// <param name="loc"></param>
		public static void DrawString(string str, Font font, Color color, PointF loc)
		{
			DrawString(str, color, loc);
		}

		public static void DrawString(string str, Color color, PointF loc)
		{
			SetGLColor(color);
			Gl.glRasterPos3f(loc.X, loc.Y, .1f);
			glPrint(str);
		}

		public static void DrawStringMultiLine(string str, Color color, PointF loc, GLCamera cam)
		{
			//split the string by newlines
			string[] strings = str.Split('\n');
			float ypos = loc.Y;
			foreach (string s in strings)
			{				
				SetGLColor(color);
				Gl.glRasterPos3f(loc.X, ypos,.1f);
				glPrint(s);
				ypos -= (MeasureString(s, GLUtility.defFault).Height/1.8f) / cam.scale;
			}
		}
				
		//jacked code!
		private static void glPrint(string text)
		{
			if (text == null || text.Length == 0) return; 
			Gl.glPushAttrib(Gl.GL_LIST_BIT);
			Gl.glListBase(fontbase - 32);
			
			byte[] textbytes = new byte[text.Length];
			for (int i = 0; i < text.Length; i++) textbytes[i] = (byte)text[i];
			Gl.glCallLists(text.Length, Gl.GL_UNSIGNED_BYTE, textbytes);
			Gl.glPopAttrib();
		}

		static bool initedTextures = false;
		public static bool LoadNewGLTexture(string filename, out int textID)
		{
			//lazy init textures...
			if (initedTextures == false)
			{
				Gl.glGenTextures(maxNumTextures, textures);
				initedTextures = true;
			}

			Bitmap textureImage = new Bitmap(filename);
			if (textureImage == null) { textID = -1; return false; }
						
			textureImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
			Rectangle rectangle = new Rectangle(0, 0, textureImage.Width, textureImage.Height);
			BitmapData bitmapData = textureImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			// Typical Texture Generation Using Data From The Bitmap
			textID = textures[curTextID];
			Console.WriteLine("Loaded texture id: " + textures[curTextID]);
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, textures[curTextID++]);
			
			Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB8, textureImage.Width, textureImage.Height, 0, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
			Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
			
			textureImage.UnlockBits(bitmapData);
			textureImage.Dispose();			
			return true;
		}
		


		private static void glBuildFont()
		{
			IntPtr font;
			IntPtr oldfont;
			fontbase = Gl.glGenLists(96);

			font = Gdi.CreateFont( 
					-10,                                                            // Height Of Font
					0,                                                              // Width Of Font
					0,                                                              // Angle Of Escapement
					0,                                                              // Orientation Angle
					Gdi.FW_BOLD,                                                    // Font Weight
					false,                                                          // Italic
					false,                                                          // Underline
					false,                                                          // Strikeout
					Gdi.ANSI_CHARSET,                                               // Character Set Identifier
					Gdi.OUT_TT_PRECIS,                                              // Output Precision
					Gdi.CLIP_DEFAULT_PRECIS,                                        // Clipping Precision
					Gdi.ANTIALIASED_QUALITY,                                        // Output Quality
					Gdi.FF_DONTCARE | Gdi.DEFAULT_PITCH,                            // Family And Pitch
					"Verdana");																											// Font Name

			
			oldfont = Gdi.SelectObject(hDC, font);                              // Selects The Font We Want
			Wgl.wglUseFontBitmaps(hDC, 32, 96, fontbase);                       // Builds 96 Characters Starting At Character 32
			Gdi.SelectObject(hDC, oldfont);                                     // Selects The Font We Want
			Gdi.DeleteObject(font);                                            // Delete The Font			
		}

		#region Transformations
		public static void GoToTransform(WorldTransform wt)
		{
			Gl.glPushMatrix();
			Gl.glTranslatef((float)wt.CenterPoint.X, (float)wt.CenterPoint.Y, 0.0f);
		}

		public static void GoToTransform(float x, float y)
		{
			Gl.glPushMatrix();
			Gl.glTranslatef(x,y, 0.0f);
		}

    public static void GoToTransformXYZ(float x, float y, float z)
    {
      Gl.glPushMatrix();
      Gl.glTranslatef(x, y, z);
    }

		public static void GoToTransform(float rotRadians)
		{
			Gl.glPushMatrix();
			Gl.glRotatef((rotRadians * 180 / (float)Math.PI), 0.0f, 0.0f, 1.0f);
		}

    public static void GoToTransformYPR(float Yrad, float Prad, float Rrad)
    {
      Gl.glPushMatrix();
      Gl.glRotatef((Yrad * 180 / (float)Math.PI), 0.0f, 0.0f, 1.0f);
      Gl.glRotatef((Prad * 180 / (float)Math.PI), 0.0f, 1.0f, 0.0f);
      Gl.glRotatef((Rrad * 180 / (float)Math.PI), 1.0f, 0.0f, 0.0f);
    }


		public static void ComeBackFromTransform()
		{
			Gl.glPopMatrix();
		}

		public static void GoToVehicleCoordinates(float heading, PointF pos, bool rotate90)
		{
			Gl.glPushMatrix(); //put the current matrix on the stack
			Gl.glTranslatef(pos.X, pos.Y, 0);
			if (rotate90)
				Gl.glRotatef((heading * 180 / (float)Math.PI - 90), 0.0f, 0.0f, 1.0f);
			else
				Gl.glRotatef((heading * 180 / (float)Math.PI), 0.0f, 0.0f, 1.0f);
		}

		public static void GoToVehicleCoordinates(float heading, PointF pos)
		{
			GoToVehicleCoordinates(heading, pos, true);
		}

		public static void ComeBackFromVehicleCoordinates()
		{
			Gl.glPopMatrix();
		}

		#endregion

		#region GL boolstuff

		public static void EnableNiceLines()
		{
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
		}

		public static void DisableNiceLines()
		{
			Gl.glDisable(Gl.GL_LINE_SMOOTH);
		}

		public static void SetGLColor(Color color)
		{
			Gl.glColor3ub(color.R, color.G, color.B);
		}

		public static void SetGLColor(Color color, float alpha)
		{
			Gl.glColor4ub (color.R, color.G, color.B, (byte)(alpha * 255.0f));
		}

		public static void InitGL(int width, int height, Color clearColor, SimpleOpenGlControl ctrl, bool fast)
		{
			// Attempt To Get A Device Context
			hDC = User.GetDC(ctrl.Handle); 
			measureGraphics = null;

      //Gl.glClearColor(((float)clearColor.R / 255.0f), ((float)clearColor.G / 255.0f), ((float)clearColor.B / 255.0f), 0);

      float[] fogColor = {.5f, .5f, .5f, 1};                // Fog Color
      float[] lightAmbient = {0.5f, 0.5f, 0.5f, 1};
      float[] lightDiffuse = {1, 1, 1, 1};
      float[] lightPosition = {0, 0, 30, 1};

      float[] mat_diffuse = {0.1f, 0.5f, 0.8f, 1.0f};
      float[] no_mat = { 0.0f, 0.0f, 0.0f, 1.0f };
      Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_DIFFUSE, mat_diffuse);
      Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_SPECULAR, no_mat);
      Gl.glMaterialf(Gl.GL_FRONT, Gl.GL_SHININESS, 0.0f);
      Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_EMISSION, no_mat);

      Gl.glShadeModel(Gl.GL_SMOOTH);                                      // Enable Smooth Shading
      Gl.glClearDepth(1);                                                 // Depth Buffer Setup
      Gl.glEnable(Gl.GL_DEPTH_TEST);                                      // Enables Depth Testing
      Gl.glDepthFunc(Gl.GL_LEQUAL);                                       // The Type Of Depth Testing To Do
      Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);         // Really Nice Perspective Calculations
      Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_AMBIENT, lightAmbient);            // Setup The Ambient Light
     // Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_DIFFUSE, lightDiffuse);            // Setup The Diffuse Light
      Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, lightPosition);          // Position The Light
      Gl.glEnable(Gl.GL_LIGHT1);                                          // Enable Light One
      Gl.glFogi(Gl.GL_FOG_MODE, Gl.GL_EXP);                      // Fog Mode
      Gl.glFogfv(Gl.GL_FOG_COLOR, fogColor);                              // Set Fog Color
      Gl.glFogf(Gl.GL_FOG_DENSITY, 0.3f);                                // How Dense Will The Fog Be
      Gl.glHint(Gl.GL_FOG_HINT, Gl.GL_DONT_CARE);                         // Fog Hint Value
      Gl.glFogf(Gl.GL_FOG_START, 1);                                      // Fog Start Depth
      Gl.glFogf(Gl.GL_FOG_END, 20);                                        // Fog End Depth
      
      Gl.glEnable(Gl.GL_BLEND);
      Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
      Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_DONT_CARE);
      Gl.glEnable(Gl.GL_LINE_SMOOTH);

			//if (!fast) 

      /*
			Gl.glClearDepth(1);

			Gl.glEnable(Gl.GL_BLEND);
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glEnable(Gl.GL_DEPTH_TEST);				

			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_DONT_CARE);
      Gl.glDepthFunc(Gl.GL_LEQUAL); 
			if (!fast)
			{
				Gl.glEnable(Gl.GL_LINE_STIPPLE);				
				//Gl.glShadeModel(Gl.GL_SMOOTH);
			}
			Gl.glViewport(0, 0, width, height);

			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			//Gl.glShadeModel(Gl.GL_FLAT);
      Gl.glShadeModel(Gl.GL_SMOOTH);*/


			
			//if (!fast)
			//{
				//Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
			
			//}
			Gl.glLineWidth(1.0f);
			
			glBuildFont();
		}

    public static void SetClearColor(Color clearColor)
    {
      Gl.glClearColor(((float)clearColor.R / 255.0f), ((float)clearColor.G / 255.0f), ((float)clearColor.B / 255.0f), 0);
    }

		public static void InitGL(int width, int height, Color clearColor, SimpleOpenGlControl ctrl)
		{
			InitGL(width, height, clearColor, ctrl, false);
		}

		public static void ClearScreenBuf()
		{
			Gl.glLoadIdentity();
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
		}

    public static void InitProjection(GLCamera cur, GLCamera prev, double t, Size viewportSize)
    {
      Gl.glMatrixMode(Gl.GL_PROJECTION);
      Gl.glLoadIdentity();
      cur.ApplyProjection(prev, t);
      Gl.glMatrixMode(Gl.GL_MODELVIEW);
      Gl.glLoadIdentity();
    }

    public static void InitScene(GLCamera cur, GLCamera prev, double t, Size viewportSize)
    {      
      Gl.glViewport(0, 0, viewportSize.Width, viewportSize.Height);
      Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
      InitProjection(cur, prev, t, viewportSize);
    }

		public static void InitScene(WorldTransform transform)
		{
			Coordinates ll = transform.WorldLowerLeft;
			Coordinates ur = transform.WorldUpperRight;
			
			Gl.glViewport(0, 0, (int)transform.ScreenSize.Width, (int)transform.ScreenSize.Height);
			Gl.glLoadIdentity();
			Gl.glOrtho(ll.X,ur.X,ll.Y,ur.Y,-100.0,100.0);
			
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
		}
		#endregion

	/*	public static void DrawCar(PointF rearAxle, float h, int txid)
		{

			//the rear axle point is just the center of the car in x and the rear of the car in y
			PointF carPoint = new PointF(rearAxle.X - (carWidth / 2), rearAxle.Y);
			GLUtility.GoToVehicleCoordinates(h, carPoint);
			RectangleF r = new RectangleF(0, 0, carWidth, carHeight);
			GLPen pen = new GLPen(Color.LightBlue, 1.0f);
			GLUtility.FillTexturedRectangle(txid, r);
			GLUtility.ComeBackFromVehicleCoordinates();
		}
		*/

		//0 degree heading means positive X and means EAST
		public static void DrawCar(PointF rearAxle, float h, Color color, string label, bool rotate90, Color labelColor)
		{
			float carWidth = (float)TahoeParams.T;
			float carIMUtoFront = (float)(TahoeParams.FL - TahoeParams.IL);
			float carHeight = (float)TahoeParams.VL;
			//the rear axle point is just the center of the car in x and the rear of the car in y			
			PointF IMU = new PointF (rearAxle.X,rearAxle.Y);
			GLUtility.GoToVehicleCoordinates(h, IMU,rotate90);
			RectangleF r = new RectangleF(-carWidth / 2, carIMUtoFront - carHeight, carWidth, carHeight);
			GLPen pen = new GLPen(color, 1.0f);
			GLUtility.DrawRectangle(pen, r);
			GLUtility.DrawLine(pen, r.Left + 0.2f, r.Bottom - r.Width / 2, r.Left + r.Width / 2, r.Bottom - .2f);
			GLUtility.DrawLine(pen, r.Right - 0.2f, r.Bottom - r.Width / 2, r.Right - r.Width / 2, r.Bottom - .2f);
      if (label != "") GLUtility.DrawString(label, new Font("verdana", 1.0f), labelColor, new PointF(+.20f, 0));
			GLUtility.DrawCross(new GLPen (Color.Pink,1.0f), new PointF(0, 0), r.Width / 2);
			GLUtility.ComeBackFromVehicleCoordinates();
		}

		public static void DrawBox(PointF p, float h, float width, float length, string label, bool rotate90, DrawRef drawRef, Color color)
		{
			GLUtility.GoToVehicleCoordinates(h, p, rotate90);
			RectangleF r = new RectangleF();
			if (drawRef == DrawRef.RearAxle)
				r = new RectangleF(-width / 2, 0, width, length);
			else if (drawRef == DrawRef.Center)
				r = new RectangleF(-width / 2, -length / 2, width, length);
			else if (drawRef == DrawRef.BottomCorner)
				r = new RectangleF(0, 0, width, length);
			GLPen pen = new GLPen(color, 1.0f);
			GLUtility.DrawRectangle(pen, r);
			GLUtility.DrawLine(pen, r.Left + 0.2f, r.Bottom - r.Width / 2, r.Left + r.Width / 2, r.Bottom - .2f);
			GLUtility.DrawLine(pen, r.Right - 0.2f, r.Bottom - r.Width / 2, r.Right - r.Width / 2, r.Bottom - .2f);
			if (label != "") GLUtility.DrawString(label, new Font("verdana", 1.0f), Color.Black, r.Location);
			GLUtility.ComeBackFromVehicleCoordinates();
		}


		public static void DrawBox(PointF p, float h, float width, float length, string label, bool rotate90, DrawRef drawRef)
		{
			DrawBox(p, h, width, length, label, rotate90, drawRef, Color.Tomato);
		}

		public static void DrawCluster(PointF p, float h, float width, string label, bool rotate90)
		{
			GLUtility.GoToVehicleCoordinates(h, p, rotate90);
			GLPen pen = new GLPen(Color.Blue, 1.0f);
			GLUtility.DrawLine(pen,-width/2,0,width/2,0);
			GLUtility.DrawLine(pen, 0, .1f, 0, -.1f);
			if (label != "") GLUtility.DrawString(label, new Font("verdana", 1.0f), Color.Black, new PointF(0,0));
			GLUtility.ComeBackFromVehicleCoordinates();
		}

    public static void DrawGrid(int gridStep, WorldTransform w, bool drawRadii)
    {
      DrawGrid(gridStep, w, drawRadii, Color.LightGray, Color.LightGray, Color.LightPink);
    }

    public static void DrawGrid3D(int gridStep, WorldTransform w, float z, Color lineColor, Color textColor, Color radiiColor)
    {
      GLUtility.DisableNiceLines();
      GLPen pen = new GLPen(lineColor, 1.0f);
			pen.GLApplyPen();
			Gl.glBegin(Gl.GL_LINES);

			for (int i = (int)w.WorldLowerLeft.Y; i < w.WorldUpperRight.Y; i++)
      {
        if (i % gridStep == 0)
        {
					Gl.glVertex3f((float)w.WorldLowerLeft.X, i, z);
					Gl.glVertex3f((float)w.WorldUpperRight.X, i, z);					          
          //GLUtility.DrawString(i.ToString() + "m", new Font("verdana", 1), textColor, new PointF(0, i + .25f));
        }
      }
      for (int i = (int)w.WorldLowerLeft.X; i < w.WorldUpperRight.X; i++)
      {
        if (i % gridStep == 0)
        {
					Gl.glVertex3f(i,(float)w.WorldLowerLeft.Y,z);
					Gl.glVertex3f(i, (float)w.WorldUpperRight.Y, z);					                    
          //GLUtility.DrawString(i.ToString() + "m", new Font("verdana", 1), textColor, new PointF(i, (float)(w.WorldUpperRight.Y - w.WorldLowerLeft.Y) / 2.0f));
        }
      }
			Gl.glEnd();
      GLUtility.EnableNiceLines();
    }


		public static void DrawGrid(int gridStep, WorldTransform w, bool drawRadii, Color lineColor, Color textColor, Color radiiColor)
		{
			GLUtility.DisableNiceLines();
			for (int i = (int)w.WorldLowerLeft.Y; i < w.WorldUpperRight.Y; i ++)
			{
				if (i % gridStep == 0)
				{
          GLUtility.DrawLine(new GLPen(lineColor, 1.0f), (float)w.WorldLowerLeft.X, i, (float)w.WorldUpperRight.X, i);
          GLUtility.DrawString(i.ToString() + "m", new Font("verdana", 1), textColor, new PointF(0, i + .25f));
				}
			}
			for (int i = (int)w.WorldLowerLeft.X; i < w.WorldUpperRight.X; i ++)
			{
				if (i % gridStep == 0)
				{
          GLUtility.DrawLine(new GLPen(lineColor, 1.0f), i, (float)w.WorldLowerLeft.Y, i, (float)w.WorldUpperRight.Y);
          GLUtility.DrawString(i.ToString() + "m", new Font("verdana", 1), textColor, new PointF(i, (float)(w.WorldUpperRight.Y - w.WorldLowerLeft.Y) / 2.0f));
				}
			}
			
			if (drawRadii)
			{
				RectangleF r = new RectangleF(new PointF(-gridStep, -gridStep), new SizeF(gridStep * 2.0f, gridStep * 2.0f));
				for (int i = 0; i < 20; i++)
				{
					r.Inflate(gridStep, gridStep);
          GLUtility.DrawEllipse(new GLPen(radiiColor, 1), r);
				}
			}
			GLUtility.EnableNiceLines();
		}
	}

	public class GLPen 
	{
		public Color color;
		public float width=1.0f;
		public DashStyle DashStyle;
   
		public GLPen(Color color, float width)
		{
			this.color = color;
			this.width = width;
		}

		public void GLApplyPen()
		{
			if (width < 1.0f) width = 1.0f;
			Gl.glColor4ub(color.R, color.G, color.B, color.A);

      float[] c = { (float)color.R / 255.0f, (float)color.G / 255.0f, (float)color.B / 255.0f, (float)color.A/255.0f };
      Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE, c);
			
      Gl.glLineWidth(width*1.5f);
			if (DashStyle != DashStyle.Solid)
				Gl.glLineStipple(1, (short)0x00FF);
			else
				Gl.glLineStipple(1, -1);	
		}
	}
}
