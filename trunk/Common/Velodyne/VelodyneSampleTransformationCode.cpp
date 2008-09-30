//
// The following are some example C++ snippets that show how to parse a packet
// from the Velodyne Lidar Scanner and convert the data time of flight distance
// information into a 3D position.
//

//
// Various constants used in the example code:
//
const guint VLS_PORT = 2368;             // UDP port for broadcast of packets from scanner
const guint VLS_PKT_LEN = 1248;          // Total length of a lidar packet
const guint VLS_DATA_LEN_V1 = 1216;      // Payload size of old lidar packet (not used anymore)
const guint VLS_DATA_LEN_V2 = 1214;      // Payload size of new lidar packet
const guint VLS_TRAILER_LEN = 6;         // Length of trailer data
const guint VLS_LINK_HDR_LEN = 14;       // Length of ethernet header
const guint VLS_IP_VERSION = 4;          // IPv4
const guint VLS_IP_PROTO = 17;           // UDP
const guint VLS_HDR_LEN = 42;            // Length of lidar packet headers (before payload)
const guint VLS_FIRING_PER_PKT = 12;     // Number of firings per packet
const guint VLS_LASER_PER_FIRING = 32;   // Number of lasers in a firing
const guint VLS_MAX_NUM_LASERS = 64;     // Total number of lasers
const guint VLS_NUM_ROT_ANGLES = 36000;  // Number of rotation values (0 through 35999(
const guint VLS_NUM_BLOCKS = 2;          // Number of "firing blocks" (lasers 0-31 and 32-63)

// Enum for the "select" value used to descriminate between firing blocks
enum LaserBlockSelect {
	BLOCK_0_TO_31  = 0xeeff,
	BLOCK_32_TO_63 = 0xddff
};

//
// Data structures for data within a packet's payload.  The payload for a packet
// consists of VLS_FIRING_PER_PKT number of vls_firing structs which contain
// the firing data.
//
/** Laser data point. */
typedef struct vls_point {
	guint16 distance;
	guint8 intensity;
} __attribute__((packed)) vls_point_t;

/** Firing data from a set of lasers. */
typedef struct vls_firing {
	guint16 select; // which firing block this is from, block 0 (lasers 0-31) has a value
                        //  of 0xeeff while block 1 (lasers 32-63) has a value of 0xddff
	guint16 position;

	vls_point_t points[VLS_LASER_PER_FIRING];
} __attribute__((packed)) vls_firing_t;

//
// Function to sanity check a packet header to make sure it is valid.  These
// checks are overly paranoid and can probably be omitted, but are good for
// validating new code.  This also shows what the each packet header is comprised
// of and at the end a pointer is returned which points to the data payload.
//
guchar *
sanityCheckPacket(boost::shared_ptr<PcapData> pkt) throw (VlsException)
{
	// Check that the packet size is expected
	if (pkt->getPktLen() != VLS_PKT_LEN || pkt->getPktCapLen() != VLS_PKT_LEN) {
		std::stringstream errStr;
		errStr << "Packet is incorrect size " << pkt->getPktLen();
		throw VlsException(errStr.str());
	}

	// Advance the buffer pointer past the link level header
	guchar *ptr = pkt->getPktData().get() + VLS_LINK_HDR_LEN;

	// Check that IP header makes sense
	struct ip *ip_hdr = (struct ip *)ptr;
	if (ip_hdr->ip_v != VLS_IP_VERSION) // check IP version
		throw VlsException("Invalid IP version");

	if (ip_hdr->ip_p != VLS_IP_PROTO) // check that the protocol is UDP
		throw VlsException("Invalid protocol");

	// Advance the buffer pointer past the IP header
	ptr += 4 * ip_hdr->ip_hl;

	// Check that UDP header makes sense
	struct udphdr *udp_hdr = (struct udphdr *)ptr;
	if (ntohs(udp_hdr->dest) != VLS_PORT)
		throw VlsException("Invalid UDP destination port");

	guint16 len = ntohs(udp_hdr->len);
	if (len != VLS_DATA_LEN_V1 && len != VLS_DATA_LEN_V2) {
		std::stringstream errStr;
		errStr << "Invalid data payload length " << ntohs(udp_hdr->len);
		throw VlsException(errStr.str());
	}

	// Advance the buffer pointer past the UDP header
	ptr += sizeof(struct udphdr);

	return ptr;
}

//
// Function to add individual packets to a scan structure and determine the
// boundary between scans.  In this code the notion of a scan is all packets
// within one rotation (from 0 to 359 degrees), although this is an arbitrary
// boundary.
//
void
ScanBuilder::addPkt(boost::shared_ptr<PcapData> pkt, const fpos_t &pos) throw (VlsException)
{
	// First check that this isn't out of order
	if (pkt->getPktTimestamp() < lastTimestamp_) {
		std::stringstream errStr;
		errStr << "New packet is out of order: last " << lastTimestamp_ << " cur " << pkt->getPktTimestamp();
		throw VlsException(errStr.str());
	}

	// Sanity check the packet
	guchar *ptr = sanityCheckPacket(pkt); // can throw

	// Process the laser samples
	guint numFirings = 0;
	for (guint i=0; i < VLS_FIRING_PER_PKT; i++, numFirings++) {
		// Map the firing structure into the packet
		vls_firing_t *hdr = (vls_firing_t *)ptr;

		// Check the position to see if we have wrapped around
		if (hdr->position < lastPosition_) {
			// Push the current packet into the scan if it isn't first sample
			if (numFirings != 0)
				curScan_->addPacket(pkt, pos, numFirings);

			// Emit constructed last Packet
			if (!curScan_->empty())
				newScanSignal_.emit(curScan_);

			// Create a new scan structure, increase index
			numFirings = 0;
			curScan_ = boost::shared_ptr<Scan>(new Scan(pcap_, db_));
		}

		// Advance pointer after checking for wrap around
		ptr += sizeof(vls_firing_t);

		// Update our position
		lastPosition_ = hdr->position;
	}

	// Push the packet into the current Scan
	if (numFirings != 0)
		curScan_->addPacket(pkt, pos, numFirings);

	// Don't forget to update the timestamp
	lastTimestamp_ = pkt->getPktTimestamp();
}

//
// Function to convert time of flight information into 3D position.  This isn't
// particularly optimized, for clarity sake, other than that most sine and cosine values have been
// precomputed.  The "db" object holds the calibration data for each laser
// and the "pos" object holds the current 3D position of the scanner (i.e. from GPS).
// This code doesn't factor in orientation of the scanner (roll, pitch, yaw), but it is
// fairly straightforward to factor these values in.  The physical offsets between the two laser blocks
// is not currently taken into account, but adding these corrections is fairly straightforward.  Please
// note that this code is structured try and make it clear about how the various corrections are applied
// and is not the most efficient implementation.
// 
// The following correction values are used:
//
// distLSB - the CM value for the lsb of a time of flight distance reading
// vertCorrection - the elevation angle for each laser (positive rotates the beam towards the top of the scanner)
// rotCorrection - the azimuth angle offset for each laser (offset from the current rotation angle, positive rotates the beam counter-clockwise)
// distCorrection - a CM distance offset applied to the time of flight distance given by laser
// horizOffsetCorrection - a horizontal parallax correction (orthogonal to laser beam)
// vertOffsetCorrection - a vertical parallax correction (orthogonal to laser beam)
//
// *Note: If the beam were initially pointed directly along +y axis of the world frame, a positive horizontal offset
//        correction would shift the beam to the left (-x) and a positive vertial offset correction correction
//        whould shift the beam up (+z)
//
// In these computations the positive Y-axis is at rotational degree zero and the scanner
// rotates clockwise around the Z-axis.
//
void
firingData::computeCoords(guint16 laserNum, boost::shared_ptr<CalibrationDB> db, GLpos_t &pos)
{
	guint16 idx = laserNum % VLS_LASER_PER_FIRING;
	boost::shared_ptr<CalibrationPoint> cal = db->getCalibration(laserNum);

	if (data->points[idx].distance == 0) {
		coords[idx].setX(0.0);
		coords[idx].setY(0.0);
		coords[idx].setZ(0.0);
		return;
	}

	float distance = (db->getDistLSB() * (float)data->points[idx].distance) + cal->getDistCorrection();

	float cosVertAngle = cal->getCosVertCorrection();
	float sinVertAngle = cal->getSinVertCorrection();
	float cosRotCorrection = cal->getCosRotCorrection();
	float sinRotCorrection = cal->getSinRotCorrection();

	// cos(a-b) = cos(a)*cos(b) + sin(a)*sin(b)
	// sin(a-b) = sin(a)*cos(b) - cos(a)*sin(b)
	float cosRotAngle = rotCosTable[data->position]*cosRotCorrection + rotSinTable[data->position]*sinRotCorrection;
	float sinRotAngle = rotSinTable[data->position]*cosRotCorrection - rotCosTable[data->position]*sinRotCorrection;

	distance /= VLS_DIM_SCALE;

	// The offset corrections are to be applied in planes orthogonal to the rotatation corrected beam
	float hOffsetCorr = cal->getHorizOffsetCorrection()/VLS_DIM_SCALE;
	float vOffsetCorr = cal->getVertOffsetCorrection()/VLS_DIM_SCALE;

//                           / (distance, shifted by vertical offset)
//    z ^                  /
//      |                /
//      --> y          /          distance                                             
//     /             /           /            vertOffsetCorrection                      
//    v x          /           /            +-----------+                              
//               /           /              |     ^     |                             
//             /           /                |     |     |                         
// vertOffset/           /                  |<----o     | horizOffsetCorrection
//          \          /                    |           |
//           \ 90deg /                      |           |
//            \    /         xyDist         +-----------+
//    90-theta \ /   theta   |              Note: the "o" represents the beam pointing into the screen
//         ------------------------               if the beam were aligned with +y in the world frame
//           ^  |         ^                       then vertOffset would be aligned with +z and hoizOffset
//           |     distance*cos(theta)            would be aligned with -x
//  vertOffset*cos(90-theta) = vertOffset*sin(theta)
//
// Note: theta = vertCorrection angle
//
//                   / (x,y)
//                 /
//     y         / 
//    ^        /           xyDist
//    |      /   |       /
//    -->x   \   |theta/
//            \  |   /
//   horizOff  \ | / 
//           ----/-------- xyDist*sin(theta)
//
// Note: theta = rotCorrection angle

	// Compute the distance in the xy plane (without accounting for rotation)
	float xyDistance = distance * cosVertAngle - vOffsetCorr * sinVertAngle;

	// pos is the position of the scanner, factor in rotation angle and horizontal offset
	coords[idx].setX(xyDistance * sinRotAngle - hOffsetCorr * cosRotAngle + pos.getX()/VLS_DIM_SCALE);
	coords[idx].setY(xyDistance * cosRotAngle + hOffsetCorr * sinRotAngle + pos.getY()/VLS_DIM_SCALE);
	coords[idx].setZ(distance * sinVertAngle + vOffsetCorr * cosVertAngle + pos.getZ()/VLS_DIM_SCALE);
}