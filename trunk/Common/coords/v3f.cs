using System;
namespace UrbanChallenge.Common
{
  public struct v3f
  {
    public v3f(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }
    public v3f(float Q)
    {
      x = y = z = Q;
    }
    public float x, y, z;
    public static v3f operator *(v3f a, float b)
    {
      return new v3f(a.x * b, a.y * b, a.z * b);
    }
    public static v3f operator -(v3f a, v3f b)
    {
      return new v3f(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static v3f operator +(v3f a, v3f b)
    {
      return new v3f(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public v3f cross(v3f b)
    {
      v3f res;
      res.x = y * b.z - z * b.y;
      res.y = z * b.x - x * b.z;
      res.z = x * b.y - y * b.x;
      return res;
    }
    public float mag()
    {
      return (float)Math.Sqrt(x * x + y * y + z * z);
    }
    public v3f norm()
    {
      float m = mag();
      return new v3f(x / m, y / m, z / m);
    }
    public v3f rotateAbout(v3f axis, float rotationRad)
    {
      v3f res = new v3f();
      v3f uvw = axis.norm();
      //http://www.mines.edu/~gmurray/ArbitraryAxisRotation/ArbitraryAxisRotation.htm
      float u = uvw.x;
      float v = uvw.y;
      float w = uvw.z;
      float SIN = (float)Math.Sin(rotationRad);
      float COS = (float)Math.Cos(rotationRad);
      float u2 = u * u;
      float v2 = v * v;
      float w2 = w * w;
      float ux = u * x;
      float vy = v * y;
      float wz = w * z;
      float dot = ux + vy + wz;
      // x=u(ux+vy+wz)+(x(v*v+w*w)+u(-vy-wz))cos(theta)+mag(uvw)(vz-wy)sin(theta)
      res.x = u * dot + COS * (x * (v2 + w2) + u * (-vy - wz)) + uvw.mag() * (v * z - w * y) * SIN;
      res.y = v * dot + COS * (y * (u2 + w2) + v * (-ux - wz)) + uvw.mag() * (w * x - u * z) * SIN;
      res.z = w * dot + COS * (z * (u2 + v2) + w * (-ux - vy)) + uvw.mag() * (u * y - v * x) * SIN;

      return res;
    }
  }
}