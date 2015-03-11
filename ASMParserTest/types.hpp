#pragma once

#include <iostream>
#include <stddef.h>
#include <stdlib.h>
#include <time.h>

#include <map>

typedef unsigned char byte;
typedef unsigned short int ushort;
typedef unsigned long int ulong;
typedef signed long int slong;

union FP32
{
	FP32(slong v = 0) :value(v){}

	FP32(short s, ushort f)
		:shortValue(s),
		fract(f)
	{}

	FP32(byte b1, byte b2, byte b3, byte b4)
		:b1(b1), b2(b2), byteValue(b3), b4(b4)
	{}

	operator slong() const { return value; }

	slong value; // fixed point 16.16
	unsigned long int uvalue;
	struct{
		ushort fract;
		signed short int shortValue;
	};
	struct{
		byte b1, b2, byteValue, b4;
	};
}; // TODO: define different layouts to handle endianness

typedef struct VMState
{
	ushort IP; // instruction pointer
	bool ZF; // zero flag
	bool OF; // overflow flag
	bool AF; // above flag
	byte* vmMemory;
	short memSize;

	bool term;
	clock_t startTime;

	FP32 reg[255];

} VMState;

typedef std::tuple < ushort, byte, FP32 > instruction;

#define NAMEMAXSIZE 10
#define ARG1MAXSIZE 30
#define ARG2MAXSIZE 30

