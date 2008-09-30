using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UrbanChallenge.Common;
using System.Diagnostics;

namespace PathSmootherTest {
	public class PathSmootherInterface {
		private const int SR_SUCCESS = 0;
		private const int SR_FAILURE = 2;
		private const int SR_INFEAS = 1;
		private const int SR_INSUF_SPACE = 3;

		private const int SR_OPT_LOQO = 1;
		private const int SR_OPT_IPOPT = 2;

		[StructLayout(LayoutKind.Sequential, Pack=4)]
		public class smoother_options {
			// algorithm to use
			public int alg;
			
			// first-pass weighting values
			public double alpha_c0;
			public double alpha_d0;
			public double alpha_w0;

			// second-pass weighting values
			public double alpha_c1;
			public double alpha_d1;
			public double alpha_w1;

			// maximum curvature
			public double k_max;
			
			// velocity
			public double v;
			// mat lateral accel
			public double max_a_lat;
		}

		[DllImport("PathSmoother.dll", CallingConvention=CallingConvention.Cdecl)]
		private static extern int smooth_path(
			[In, MarshalAs(UnmanagedType.LPArray)] double[] px, [In, MarshalAs(UnmanagedType.LPArray)] double[] py, [In] int n_p, 
			[In, MarshalAs(UnmanagedType.LPArray)] double[] ub_x, [In, MarshalAs(UnmanagedType.LPArray)] double[] ub_y, [In] int n_ub, 
			[In, MarshalAs(UnmanagedType.LPArray)] double[] lb_x, [In, MarshalAs(UnmanagedType.LPArray)] double[] lb_y, [In] int n_lb, 
			[In] smoother_options opt, 
			[In, Out, MarshalAs(UnmanagedType.LPArray)] double[] sm_x, [In, Out, MarshalAs(UnmanagedType.LPArray)] double[] sm_y, [In, Out] ref int n_sm);

		public enum PathSmoothResult {
			Success = 0,
			Infeasible=1,
			Failure = 2,
			InsufficientSpace = 3,
			FailedToConverge = 4
		}

		public enum SmoothingAlg {
			LOQO = 1,
			IPOPT = 2
		}

		public static smoother_options DefaultOptions {
			get {
				smoother_options opt = new smoother_options();
				opt.alg = (int)(SmoothingAlg.IPOPT);

				// initialize defaults
				opt.alpha_c0 = 10;
				opt.alpha_d0 = 1;
				opt.alpha_w0 = 0.001;

				opt.alpha_c1 = 10;
				opt.alpha_d1 = 10;
				opt.alpha_w1 = 0.001;

				opt.k_max = 0.2;
				opt.v = 4.5;
				opt.max_a_lat = 3;

				return opt;
			}
		}

		public static PathSmoothResult SmoothPath(List<Coordinates> path_pts, List<Coordinates> ub_pts, List<Coordinates> lb_pts, List<Coordinates> smoothed_path, SmoothingAlg alg) {
			smoother_options opt = DefaultOptions;
			opt.alg = (int)alg;

			return SmoothPath(path_pts, ub_pts, lb_pts, smoothed_path, opt);
		}

		public static PathSmoothResult SmoothPath(List<Coordinates> path_pts, List<Coordinates> ub_pts, List<Coordinates> lb_pts, List<Coordinates> smoothed_path, smoother_options opt) {
			Stopwatch sw = Stopwatch.StartNew();
			double[] px = new double[path_pts.Count];
			double[] py = new double[path_pts.Count];

			double[] ub_x = new double[ub_pts.Count];
			double[] ub_y = new double[ub_pts.Count];

			double[] lb_x = new double[lb_pts.Count];
			double[] lb_y = new double[lb_pts.Count];

			for (int i = 0; i < path_pts.Count; i++) {
				px[i] = path_pts[i].X;
				py[i] = path_pts[i].Y;
			}

			for (int i = 0; i < ub_pts.Count; i++) {
				ub_x[i] = ub_pts[i].X;
				ub_y[i] = ub_pts[i].Y;
			}

			for (int i = 0; i < lb_pts.Count; i++) {
				lb_x[i] = lb_pts[i].X;
				lb_y[i] = lb_pts[i].Y;
			}

			// allocate space for the return points
			double[] smoothed_x = new double[300];
			double[] smoothed_y = new double[300];

			int len = 300;

			int ret = smooth_path(px, py, px.Length, ub_x, ub_y, ub_x.Length, lb_x, lb_y, lb_x.Length, opt, smoothed_x, smoothed_y, ref len);

			smoothed_path.Clear();
			smoothed_path.Capacity = len;
			for (int i = 0; i < len; i++) {
				smoothed_path.Add(new Coordinates(smoothed_x[i], smoothed_y[i]));
			}

			sw.Stop();

			Console.WriteLine("took " + sw.ElapsedMilliseconds + " ms");

			return (PathSmoothResult)ret;
		}
	}
}
