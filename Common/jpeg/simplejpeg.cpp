#include "simplejpeg.h"

#define  descale(x,n)  (((x) + (1 << ((n)-1))) >> (n))
#define  saturate(x)   (uchar)(((x) & ~255) == 0 ? (x) : ~((x)>>31))


//  Standard JPEG quantization tables
static const uchar jpegTableK1_T[] =
{
    16, 12, 14, 14,  18,  24,  49,  72,
    11, 12, 13, 17,  22,  35,  64,  92,
    10, 14, 16, 22,  37,  55,  78,  95,
    16, 19, 24, 29,  56,  64,  87,  98,
    24, 26, 40, 51,  68,  81, 103, 112,
    40, 58, 57, 87, 109, 104, 121, 100,
    51, 60, 69, 80, 103, 113, 120, 103,
    61, 55, 56, 62,  77,  92, 101,  99
};


static const uchar jpegTableK2_T[] =
{
    17, 18, 24, 47, 99, 99, 99, 99,
    18, 21, 26, 66, 99, 99, 99, 99,
    24, 26, 56, 99, 99, 99, 99, 99,
    47, 66, 99, 99, 99, 99, 99, 99,
    99, 99, 99, 99, 99, 99, 99, 99,
    99, 99, 99, 99, 99, 99, 99, 99,
    99, 99, 99, 99, 99, 99, 99, 99,
    99, 99, 99, 99, 99, 99, 99, 99
};


// Standard Huffman tables

// ... for luma DCs.
static const uchar jpegTableK3[] =
{
    0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0,
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
};


// ... for chroma DCs.
static const uchar jpegTableK4[] =
{
    0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0,
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
};


// ... for luma ACs.
static const uchar jpegTableK5[] =
{
    0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125,
    0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
    0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
    0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xa1, 0x08,
    0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0,
    0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0a, 0x16,
    0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28,
    0x29, 0x2a, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
    0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
    0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
    0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
    0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
    0x7a, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
    0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
    0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7,
    0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6,
    0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5,
    0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4,
    0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
    0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea,
    0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
    0xf9, 0xfa
};

// ... for chroma ACs  
static const uchar jpegTableK6[] =
{
    0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 119,
    0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
    0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
    0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
    0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0,
    0x15, 0x62, 0x72, 0xd1, 0x0a, 0x16, 0x24, 0x34,
    0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26,
    0x27, 0x28, 0x29, 0x2a, 0x35, 0x36, 0x37, 0x38,
    0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
    0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
    0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
    0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
    0x79, 0x7a, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
    0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96,
    0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5,
    0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4,
    0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3,
    0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2,
    0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
    0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9,
    0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
    0xf9, 0xfa
};


// zigzag & IDCT prescaling (AAN algorithm) tables
static const uchar zigzag[] =
{
  0,  8,  1,  2,  9, 16, 24, 17, 10,  3,  4, 11, 18, 25, 32, 40,
 33, 26, 19, 12,  5,  6, 13, 20, 27, 34, 41, 48, 56, 49, 42, 35,
 28, 21, 14,  7, 15, 22, 29, 36, 43, 50, 57, 58, 51, 44, 37, 30,
 23, 31, 38, 45, 52, 59, 60, 53, 46, 39, 47, 54, 61, 62, 55, 63,
 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63
};


static const int idct_prescale[] =
{
    16384, 22725, 21407, 19266, 16384, 12873,  8867,  4520,
    22725, 31521, 29692, 26722, 22725, 17855, 12299,  6270,
    21407, 29692, 27969, 25172, 21407, 16819, 11585,  5906,
    19266, 26722, 25172, 22654, 19266, 15137, 10426,  5315,
    16384, 22725, 21407, 19266, 16384, 12873,  8867,  4520,
    12873, 17855, 16819, 15137, 12873, 10114,  6967,  3552,
     8867, 12299, 11585, 10426,  8867,  6967,  4799,  2446,
     4520,  6270,  5906,  5315,  4520,  3552,  2446,  1247
};


#define fixb         14
#define fix(x, n)    (int)((x)*(1 << (n)) + .5)
#define fix1(x, n)   (x)
#define fixmul(x)    (x)

#define C0_707     fix( 0.707106781f, fixb )
#define C0_924     fix( 0.923879533f, fixb )
#define C0_541     fix( 0.541196100f, fixb )
#define C0_382     fix( 0.382683432f, fixb )
#define C1_306     fix( 1.306562965f, fixb )

#define C1_082     fix( 1.082392200f, fixb )
#define C1_414     fix( 1.414213562f, fixb )
#define C1_847     fix( 1.847759065f, fixb )
#define C2_613     fix( 2.613125930f, fixb )

#define fixc       12
#define b_cb       fix( 1.772, fixc )
#define g_cb      -fix( 0.34414, fixc )
#define g_cr      -fix( 0.71414, fixc )
#define r_cr       fix( 1.402, fixc )

#define y_r        fix( 0.299, fixc )
#define y_g        fix( 0.587, fixc )
#define y_b        fix( 0.114, fixc )

#define cb_r      -fix( 0.1687, fixc )
#define cb_g      -fix( 0.3313, fixc )
#define cb_b       fix( 0.5,    fixc )

#define cr_r       fix( 0.5,    fixc )
#define cr_g      -fix( 0.4187, fixc )
#define cr_b      -fix( 0.0813, fixc )

static const char jpegHeader[] = 
    "\xFF\xD8"  // SOI  - start of image
    "\xFF\xE0"  // APP0 - jfif extention
    "\x00\x10"  // 2 bytes: length of APP0 segment
    "JFIF\x00"  // JFIF signature
    "\x01\x02"  // version of JFIF
    "\x00"      // units = pixels ( 1 - inch, 2 - cm )
    "\x00\x01\x00\x01" // 2 2-bytes values: x density & y density
    "\x00\x00"; // width & height of thumbnail: ( 0x0 means no thumbnail)

#define postshift 14

// FDCT with postscaling
static void aan_fdct8x8( int *src, int *dst,
                         int step, const int *postscale )
{
    int  workspace[64], *work = workspace;
    int  i;

    // Pass 1: process rows
    for( i = 8; i > 0; i--, src += step, work += 8 )
    {
        int x0 = src[0], x1 = src[7];
        int x2 = src[3], x3 = src[4];

        int x4 = x0 + x1; x0 -= x1;
        x1 = x2 + x3; x2 -= x3;
    
        work[7] = x0; work[1] = x2;
        x2 = x4 + x1; x4 -= x1;

        x0 = src[1]; x3 = src[6];
        x1 = x0 + x3; x0 -= x3;
        work[5] = x0;

        x0 = src[2]; x3 = src[5];
        work[3] = x0 - x3; x0 += x3;

        x3 = x0 + x1; x0 -= x1;
        x1 = x2 + x3; x2 -= x3;

        work[0] = x1; work[4] = x2;

        x0 = descale((x0 - x4)*C0_707, fixb);
        x1 = x4 + x0; x4 -= x0;
        work[2] = x4; work[6] = x1;

        x0 = work[1]; x1 = work[3];
        x2 = work[5]; x3 = work[7];

        x0 += x1; x1 += x2; x2 += x3;
        x1 = descale(x1*C0_707, fixb);

        x4 = x1 + x3; x3 -= x1;
        x1 = (x0 - x2)*C0_382;
        x0 = descale(x0*C0_541 + x1, fixb);
        x2 = descale(x2*C1_306 + x1, fixb);

        x1 = x0 + x3; x3 -= x0;
        x0 = x4 + x2; x4 -= x2;

        work[5] = x1; work[1] = x0;
        work[7] = x4; work[3] = x3;
    }

    work = workspace;
    // pass 2: process columns
    for( i = 8; i > 0; i--, work++, postscale += 8, dst += 8 )
    {
        int  x0 = work[8*0], x1 = work[8*7];
        int  x2 = work[8*3], x3 = work[8*4];

        int  x4 = x0 + x1; x0 -= x1;
        x1 = x2 + x3; x2 -= x3;
    
        work[8*7] = x0; work[8*0] = x2;
        x2 = x4 + x1; x4 -= x1;

        x0 = work[8*1]; x3 = work[8*6];
        x1 = x0 + x3; x0 -= x3;
        work[8*4] = x0;

        x0 = work[8*2]; x3 = work[8*5];
        work[8*3] = x0 - x3; x0 += x3;

        x3 = x0 + x1; x0 -= x1;
        x1 = x2 + x3; x2 -= x3;

        dst[0] = descale(x1*postscale[0], postshift);
        dst[4] = descale(x2*postscale[4], postshift);

        x0 = descale((x0 - x4)*C0_707, fixb);
        x1 = x4 + x0; x4 -= x0;

        dst[2] = descale(x4*postscale[2], postshift);
        dst[6] = descale(x1*postscale[6], postshift);

        x0 = work[8*0]; x1 = work[8*3];
        x2 = work[8*4]; x3 = work[8*7];

        x0 += x1; x1 += x2; x2 += x3;
        x1 = descale(x1*C0_707, fixb);

        x4 = x1 + x3; x3 -= x1;
        x1 = (x0 - x2)*C0_382;
        x0 = descale(x0*C0_541 + x1, fixb);
        x2 = descale(x2*C1_306 + x1, fixb);

        x1 = x0 + x3; x3 -= x0;
        x0 = x4 + x2; x4 -= x2;

        dst[5] = descale(x1*postscale[5], postshift);
        dst[1] = descale(x0*postscale[1], postshift);
        dst[7] = descale(x4*postscale[7], postshift);
        dst[3] = descale(x3*postscale[3], postshift);
    }
}

SimpleJPEG::SimpleJPEG()
{}


void  SimpleJPEG::WriteImage(const uchar* img, int width, int height, WorstStreamEver& s)
{
	WriteImage(img,width,width,height,0,1,s);
}

void  SimpleJPEG::WriteImage( const uchar* data, int step,
                                   int width, int height, int depth, int _channels, WorstStreamEver& lowstrm)
{    
		
    // encode the header and tables
    // for each mcu:
    //   convert rgb to yuv with downsampling (if color).
    //   for every block:
    //     calc dct and quantize
    //     encode block.
    int x, y;
    int i, j;
    const int max_quality = 12;
    int   quality = max_quality;
    //WMByteStream& lowstrm = m_strm.m_low_strm;
    int   fdct_qtab[2][64];
    ulong huff_dc_tab[2][16];
    ulong huff_ac_tab[2][256];
    int  channels = _channels > 1 ? 3 : 1;
    int  x_scale = channels > 1 ? 2 : 1, y_scale = x_scale;
    int  dc_pred[] = { 0, 0, 0 };
    int  x_step = x_scale * 8;
    int  y_step = y_scale * 8;
    int  block[6][64];
    int  buffer[1024];
    int  luma_count = x_scale*y_scale;
    int  block_count = luma_count + channels - 1;
    int  Y_step = x_scale*8;
    const int UV_step = 16;
    double inv_quality;

    if( quality < 3 ) quality = 3;
    if( quality > max_quality ) quality = max_quality;

    inv_quality = 1./quality;

    // Encode header
    lowstrm.PutBytes( jpegHeader, sizeof(jpegHeader) - 1 );
    
    // Encode quantization tables
    for( i = 0; i < (channels > 1 ? 2 : 1); i++ )
    {
        const uchar* qtable = i == 0 ? jpegTableK1_T : jpegTableK2_T;
        int chroma_scale = i > 0 ? luma_count : 1;
        
        lowstrm.PutWord( 0xffdb );   // DQT marker
        lowstrm.PutWord( 2 + 65*1 ); // put single qtable
        lowstrm.PutByte( 0*16 + i ); // 8-bit table

        // put coefficients
        for( j = 0; j < 64; j++ )
        {
            int idx = zigzag[j];
            int qval = Round(qtable[idx]*inv_quality);
            if( qval < 1 )
                qval = 1;
            if( qval > 255 )
                qval = 255;
            fdct_qtab[i][idx] = Round((1 << (postshift + 9))/
                                      (qval*chroma_scale*idct_prescale[idx]));
            lowstrm.PutByte( qval );
        }
    }

    // Encode huffman tables
    for( i = 0; i < (channels > 1 ? 4 : 2); i++ )
    {
        const uchar* htable = i == 0 ? jpegTableK3 : i == 1 ? jpegTableK5 :
                              i == 2 ? jpegTableK4 : jpegTableK6;
        int is_ac_tab = i & 1;
        int idx = i >= 2;
        int tableSize = 16 + (is_ac_tab ? 162 : 12);

        lowstrm.PutWord( 0xFFC4   );      // DHT marker
        lowstrm.PutWord( 3 + tableSize ); // define one huffman table
        lowstrm.PutByte( is_ac_tab*16 + idx ); // put DC/AC flag and table index
        lowstrm.PutBytes( htable, tableSize ); // put table

        bsCreateEncodeHuffmanTable( bsCreateSourceHuffmanTable(
            htable, buffer, 16, 9 ), is_ac_tab ? huff_ac_tab[idx] :
            huff_dc_tab[idx], is_ac_tab ? 256 : 16 );
    }

    // put frame header
    lowstrm.PutWord( 0xFFC0 );          // SOF0 marker
    lowstrm.PutWord( 8 + 3*channels );  // length of frame header
    lowstrm.PutByte( 8 );               // sample precision
    lowstrm.PutWord( height );
    lowstrm.PutWord( width );
    lowstrm.PutByte( channels );        // number of components

    for( i = 0; i < channels; i++ )
    {
        lowstrm.PutByte( i + 1 );  // (i+1)-th component id (Y,U or V)
        if( i == 0 )
            lowstrm.PutByte(x_scale*16 + y_scale); // chroma scale factors
        else
            lowstrm.PutByte(1*16 + 1);
        lowstrm.PutByte( i > 0 ); // quantization table idx
    }

    // put scan header
    lowstrm.PutWord( 0xFFDA );          // SOS marker
    lowstrm.PutWord( 6 + 2*channels );  // length of scan header
    lowstrm.PutByte( channels );        // number of components in the scan

    for( i = 0; i < channels; i++ )
    {
        lowstrm.PutByte( i+1 );             // component id
        lowstrm.PutByte( (i>0)*16 + (i>0) );// selection of DC & AC tables
    }

    lowstrm.PutWord(0*256 + 63);// start and end of spectral selection - for
                                // sequental DCT start is 0 and end is 63

    lowstrm.PutByte( 0 );  // successive approximation bit position 
                           // high & low - (0,0) for sequental DCT  

    // encode data
    for( y = 0; y < height; y += y_step, data += y_step*step )
    {
        for( x = 0; x < width; x += x_step )
        {
            int x_limit = x_step;
            int y_limit = y_step;
            const uchar* rgb_data = data + x*_channels;
            int* Y_data = block[0];

            if( x + x_limit > width ) x_limit = width - x;
            if( y + y_limit > height ) y_limit = height - y;

            memset( block, 0, block_count*64*sizeof(block[0][0]));
            
            if( channels > 1 )
            {
                int* UV_data = block[luma_count];

                for( i = 0; i < y_limit; i++, rgb_data += step, Y_data += Y_step )
                {
                    for( j = 0; j < x_limit; j++, rgb_data += _channels )
                    {
                        int r = rgb_data[2];
                        int g = rgb_data[1];
                        int b = rgb_data[0];

                        int Y = descale( r*y_r + g*y_g + b*y_b, fixc - 2) - 128*4;
                        int U = descale( r*cb_r + g*cb_g + b*cb_b, fixc - 2 );
                        int V = descale( r*cr_r + g*cr_g + b*cr_b, fixc - 2 );
                        int j2 = j >> (x_scale - 1); 

                        Y_data[j] = Y;
                        UV_data[j2] += U;
                        UV_data[j2 + 8] += V;
                    }

                    rgb_data -= x_limit*_channels;
                    if( ((i+1) & (y_scale - 1)) == 0 )
                    {
                        UV_data += UV_step;
                    }
                }
            }
            else
            {
                for( i = 0; i < y_limit; i++, rgb_data += step, Y_data += Y_step )
                {
                    for( j = 0; j < x_limit; j++ )
                        Y_data[j] = rgb_data[j]*4 - 128*4;
                }
            }

            for( i = 0; i < block_count; i++ )
            {
                int is_chroma = i >= luma_count;
                int src_step = x_scale * 8;
                int run = 0, val;
                int* src_ptr = block[i & -2] + (i & 1)*8;
                const ulong* htable = huff_ac_tab[is_chroma];

                aan_fdct8x8( src_ptr, buffer, src_step, fdct_qtab[is_chroma] );

                j = is_chroma + (i > luma_count);
                val = buffer[0] - dc_pred[j];
                dc_pred[j] = buffer[0];

                {
                float a = (float)val;
                int cat = (((int&)a >> 23) & 255) - (126 & (val ? -1 : 0));

                assert( cat <= 11 );
								//M_STRM!!!!
                lowstrm.PutHuff( cat, huff_dc_tab[is_chroma] );
                lowstrm.Put( val - (val < 0 ? 1 : 0), cat );
                }

                for( j = 1; j < 64; j++ )
                {
                    val = buffer[zigzag[j]];

                    if( val == 0 )
                    {
                        run++;
                    }
                    else
                    {
                        while( run >= 16 )
                        {
                            lowstrm.PutHuff( 0xF0, htable ); // encode 16 zeros
                            run -= 16;
                        }

                        {
                        float a = (float)val;
                        int cat = (((int&)a >> 23) & 255) - (126 & (val ? -1 : 0));

                        assert( cat <= 10 );
                        lowstrm.PutHuff( cat + run*16, htable );
                        lowstrm.Put( val - (val < 0 ? 1 : 0), cat );
                        }

                        run = 0;
                    }
                }

                if( run )
                {
                    lowstrm.PutHuff( 0x00, htable ); // encode EOB
                }
            }
        }
    }

    // Flush 
 //   m_strm.Flush();
		lowstrm.FlushLastBits();
    lowstrm.PutWord( 0xFFD9 ); // EOI marker
//    m_strm.Close();

    return;
}

int*  SimpleJPEG::bsCreateSourceHuffmanTable( const uchar* src, int* dst,int max_bits, int first_bits)
{
    int   i, val_idx, code = 0;
    int*  table = dst;
    *dst++ = first_bits;
    for( i = 1, val_idx = max_bits; i <= max_bits; i++ )
    {
        int code_count = src[i - 1];
        dst[0] = code_count;
        code <<= 1;
        for( int k = 0; k < code_count; k++ )
        {
            dst[k + 1] = (src[val_idx + k] << huff_val_shift)|(code + k);
        }
        code += code_count;
        dst += code_count + 1;
        val_idx += code_count;
    }
    dst[0] = -1;
    return  table;
}

bool SimpleJPEG::bsCreateEncodeHuffmanTable( const int* src, ulong* table, int max_size )
{   
    int  i, k;
    int  min_val = INT_MAX, max_val = INT_MIN;
    int  size;
    
    /* calc min and max values in the table */
    for( i = 1, k = 1; src[k] >= 0; i++ )
    {
        int code_count = src[k++];

        for( code_count += k; k < code_count; k++ )
        {
            int  val = src[k] >> huff_val_shift;
            if( val < min_val )
                min_val = val;
            if( val > max_val )
                max_val = val;
        }
    }

    size = max_val - min_val + 3;

    if( size > max_size )
    {
        assert(0);
        return false;
    }

    memset( table, 0, size*sizeof(table[0]));

    table[0] = min_val;
    table[1] = size - 2;

    for( i = 1, k = 1; src[k] >= 0; i++ )
    {
        int code_count = src[k++];

        for( code_count += k; k < code_count; k++ )
        {
            int  val = src[k] >> huff_val_shift;
            int  code = src[k] & huff_code_mask;

            table[val - min_val + 2] = (code << 8) | i;
        }
    }
    return true;
}

int SimpleJPEG::Round(double num)
{
	int cast = (int) num; //truncate
	if ((num - cast) > .5) return cast + 1;
	else return cast;
}
