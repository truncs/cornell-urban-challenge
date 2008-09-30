#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <memory.h>
#include <assert.h>

typedef unsigned char uchar;
typedef unsigned long ulong;


class WorstStreamEver;

class SimpleJPEG
{
public:
	SimpleJPEG();
	void WriteImage(const uchar* img, int step, int width, int height, int /*depth*/, int _channels, WorstStreamEver& lowstrm );
	void WriteImage(const uchar* img, int width, int height, WorstStreamEver& s);
private:
	bool bsCreateEncodeHuffmanTable( const int* src, ulong* table, int max_size );
	int* bsCreateSourceHuffmanTable( const uchar* src, int* dst,int max_bits, int first_bits );
	int Round(double num);
	static const int huff_val_shift = 20, huff_code_mask = (1 << huff_val_shift) - 1;
};

class WorstStreamEver
{
public:
	unsigned char putbuf[4];
	int putbufCurBit;

	WorstStreamEver(int maxsize)
	{		
		memory = (unsigned char*)malloc(maxsize);
		memloc= 0;		
		memset(putbuf,0x0,4);
		putbufCurBit=31;
		this->maxsize = maxsize;
	};

	~WorstStreamEver()
	{
		free (memory);
	}
	int GetLength()
	{
		return memloc;
	}
	unsigned char* GetBuffer()
	{
		return memory;
	}
	
	void PutBytes(const char* data, int bytes)
	{
		for (int i=0; i<bytes; i++)		
			PutByte(data[i]);
	}

	void PutBytes(const unsigned char* data, int bytes)
	{
		for (int i=0; i<bytes; i++)		
			PutByte(data[i]);
	}

	void PutWord(const unsigned short w)
	{    
    PutByte ((uchar)(w >> 8));
		PutByte ((uchar)w);
	}
	void PutByte(const unsigned char b)
	{
		if (memloc >= maxsize) 
		{
			printf("WARNING: WRITE PAST END OF BUFFER ATTEMPT!");
			return;
		}
		memory[memloc]= b;
		memloc++;		
	}

	void SetBit(bool bit, int bitlocation)
	{
		int arrloc = 3-bitlocation / 8;
		int offset = bitlocation%8;
		if (bit)
			putbuf[arrloc]|=0x01 << offset;	

	}
	
	//seems like val is what we write, bits is the NUMBER of bits to write
	void Put( int val, int bits )
	{
		assert( 0 <= bits && bits < 32 );
	
		for (int i=bits-1; i>=0; i--)
		{
			if ((val >> i) & 0x01)
				SetBit(1,putbufCurBit);
			
			
			if ((putbufCurBit)==0) //last write in this block
			{
				for (int i=0; i<4; i++)
				{
					PutByte (putbuf[i]);
					if (((unsigned char)putbuf[i]) == 0xff) 
						PutByte(0x00);				
				}
				memset(putbuf,0x0,4);
				putbufCurBit=32;
			}		
			
			putbufCurBit--;
			
		}	 		
	}
	void FlushLastBits()
	{
		for (int i=0; i<4; i++)
		{
			PutByte (putbuf[i]);
			if (((unsigned char)putbuf[i]) == 0xff) 
				PutByte(0x00);				
		}
	}
	void PutHuff( int val, const ulong* table )
	{
		int min_val = (int)table[0];
    val -= min_val;
    
    assert( (unsigned)val < table[1] );

    ulong code = table[val + 2];
    assert( code != 0 );
    
    Put( code >> 8, code & 255 );
	}
private:
	unsigned char* memory;	
	int memloc;
	int maxsize;

};