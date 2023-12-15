// Author: Leonardo Tazzini

#include "bit_helper.h"

uint16_t ReverseUInt16(uint16_t x)
{
	uint16_t y;
	y = (x >> 8) & 0x00ff;
	y |= (x << 8) & 0xff00;
	return y;
}
