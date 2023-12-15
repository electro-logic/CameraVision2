// Author: Leonardo Tazzini
// Functions to read/write Bytes, UInt16, etc.. to communicate with PC

#ifndef STDIO_EXT_H_
#define STDIO_EXT_H_

#include "sys/alt_stdio.h"
#include <stdint.h>
#include <unistd.h>		// write

typedef uint8_t byte;

byte		readByte();
void		readBytes(byte* data, int dataLen);
uint16_t	readUInt16();
uint16_t 	bytesToUInt16(byte* data);
void 		writeBytes(byte* data, int dataLen);
void 		writeByte(byte data);
void		writeUInt16(uint16_t data);

#endif
