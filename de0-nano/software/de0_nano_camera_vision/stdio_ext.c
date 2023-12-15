// Author: Leonardo Tazzini

#include "stdio_ext.h"

// x86 is Little Endian
// ex. 0x123 => 0x23 0x01
uint16_t readUInt16()
{
  byte data[2];
  data[0] = readByte();
  data[1] = readByte();
  return bytesToUInt16(data);
}

uint16_t bytesToUInt16(byte* data)
{
  uint16_t result = data[0];
  result |= data[1] << 8;
  return result;
}

void writeUInt16(uint16_t data)
{
	writeBytes((byte*)&data,2);
}

void readBytes(byte* data, int dataLen)
{
	int byteIndex;
	for(byteIndex = 0; byteIndex < dataLen; byteIndex++)
	{
		data[byteIndex] = readByte();
	}
}

// Alternative version of writeBytes that use STDIO
void writeBytes(byte* data, int dataLen)
{
	int remainingBytes = dataLen;
	while (remainingBytes > 0)
	{
		int writtenBytes = write(STDOUT_FILENO, data, remainingBytes);
		remainingBytes -= writtenBytes;
	}
}

/*
void writeBytes(byte* data, int dataLen)
{
	int byteIndex;
	for(byteIndex = 0; byteIndex < dataLen; byteIndex++)
	{
		writeByte(data[byteIndex]);
	}
}
*/

byte readByte()
{
	return alt_getchar();
}

void writeByte(byte data)
{
	alt_putchar(data);
}
