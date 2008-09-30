using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Dataset.Source;
using UrbanChallenge.Common;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Common.Utility;
using OperationalLayer.Pose;
using OperationalLayer.Tracing;
using UrbanChallenge.Pose;
using UrbanChallenge.Common.Pose;
using OperationalLayer.CarTime;
using UrbanChallenge.Common.Mapack;

namespace OperationalLayer.Pose {
	class PoseListener {
		private PoseClient client;
		private int packetCount;

		private CarTimestamp lastPacketTime;

		private double lastYawRate = double.NaN;

		public HeadingBiasEstimator BiasEstimator;

		public PoseListener() {
			client = new PoseClient(IPAddress.Parse("239.132.1.33"), 4839);
			client.PoseAbsReceived += client_PoseAbsReceived;
			client.PoseRelReceived += client_PoseRelReceived;

			lastPacketTime = new CarTimestamp(-10000);

			BiasEstimator = new HeadingBiasEstimator();
		}

		public void Start() {
			client.Start();
		}

		void client_PoseRelReceived(object sender, PoseRelReceivedEventArgs e) {
			OperationalTrace.WriteVerbose("got relative pose for time {0}", e.PoseRelData.timestamp);
			CarTimestamp prevTimestamp = Services.RelativePose.CurrentTimestamp;

			Services.RelativePose.PushTransform(e.PoseRelData.timestamp, e.PoseRelData.transform);

			// get the relative transform between the previous timestamp and newest timestamp
			RelativeTransform transform = Services.RelativePose.GetTransform(prevTimestamp, e.PoseRelData.timestamp);

			lastYawRate = transform.GetRotationRate().Z;

			TimeoutMonitor.MarkData(OperationalDataSource.Pose);
		}

		void client_PoseAbsReceived(object sender, PoseAbsReceivedEventArgs e) {
			packetCount++;

			Services.Dataset.MarkOperation("pose rate", LocalCarTimeProvider.LocalNow);

			//OperationalTrace.WriteVerbose("got absolute pose for time {0}", e.PoseAbsData.timestamp);

			if (lastPacketTime.ts > e.PoseAbsData.timestamp.ts) {
				lastPacketTime = new CarTimestamp(-10000);
			}

			if (e.PoseAbsData.timestamp.ts - lastPacketTime.ts >= 0.02) {
				DatasetSource ds = Services.Dataset;
				PoseAbsData d = e.PoseAbsData;
				CarTimestamp now = e.PoseAbsData.timestamp;

				if (Services.Projection != null) {
					Coordinates xy = Services.Projection.ECEFtoXY(new Vector3(d.ecef_px, d.ecef_py, d.ecef_pz));
					ds.ItemAs<Coordinates>("xy").Add(xy, now);

					AbsolutePose absPose = new AbsolutePose(xy, d.yaw, now);
					Services.AbsolutePose.PushAbsolutePose(absPose);
				}

				if (!Settings.UseWheelSpeed) {
					ds.ItemAs<double>("speed").Add(d.veh_vx, now);
				}

				ds.ItemAs<double>("vel - y").Add(d.veh_vy, now);
				ds.ItemAs<double>("heading").Add(BiasEstimator.CorrectHeading(d.yaw), now);
				ds.ItemAs<double>("pitch").Add(d.pitch + 1.25 * Math.PI/180.0, now);
				ds.ItemAs<double>("roll").Add(d.roll, now);
				ds.ItemAs<double>("ba - x").Add(d.bax, now);
				ds.ItemAs<double>("ba - y").Add(d.bay, now);
				ds.ItemAs<double>("ba - z").Add(d.baz, now);
				ds.ItemAs<double>("bw - x").Add(d.bwx, now);
				ds.ItemAs<double>("bw - y").Add(d.bwy, now);
				ds.ItemAs<double>("bw - z").Add(d.bwz, now);
				ds.ItemAs<PoseCorrectionMode>("correction mode").Add(d.correction_mode, now);

				LLACoord lla = WGS84.ECEFtoLLA(new Vector3(d.ecef_px, d.ecef_py, d.ecef_pz));
				ds.ItemAs<double>("altitude").Add(lla.alt, now);

				if (Services.Projection != null) {
					ds.ItemAs<Coordinates>("gps xy").Add(Services.Projection.ECEFtoXY(new Vector3(d.gps_px, d.gps_py, d.gps_pz)), now);
					ds.ItemAs<Coordinates>("hp xy").Add(Services.Projection.ECEFtoXY(new Vector3(d.hp_px, d.hp_py, d.hp_pz)), now);
				}

				ds.ItemAs<double>("sep heading").Add(d.sep_heading, now);
				ds.ItemAs<double>("sep pitch").Add(d.sep_pitch, now);
				ds.ItemAs<double>("sep roll").Add(d.sep_roll, now);

				if (!double.IsNaN(lastYawRate) && lastPacketTime.ts > 0) {
					// get the enu velocity
					Vector3 pECEF = new Vector3(d.ecef_px, d.ecef_py, d.ecef_pz);
					Vector3 vECEF = new Vector3(d.ecef_vx, d.ecef_vy, d.ecef_vz);
					Matrix3 Recef2enu = Geocentric.Recef2enu(pECEF);
					Vector3 vENU = Recef2enu*vECEF;
					BiasEstimator.Update(lastYawRate, d.yaw, vENU, now.ts-lastPacketTime.ts, now);

					Services.Dataset.ItemAs<double>("heading bias").Add(BiasEstimator.HeadingBias, now);
				}

				lastPacketTime = now;
			}
		}
	}
}
