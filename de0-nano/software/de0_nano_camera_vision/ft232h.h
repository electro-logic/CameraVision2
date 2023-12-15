#ifndef FT232H_H_
#define FT232H_

#include <stdio.h>
#include <stdint.h>
#include "system.h"
#include "io.h"

#define FT232H_DATA_REG	0x00
#define FT232H_CTRL_REG 0x01
#define FT232H_DL0_REG 	0x02
#define FT232H_DL1_REG 	0x03
#define FT232H_DL2_REG 	0x04
#define FT232H_DL3_REG 	0x05
#define FT232H_DI0_REG 	0x06
#define FT232H_DI1_REG 	0x07
#define FT232H_DI2_REG 	0x08
#define FT232H_DI3_REG 	0x09

#define FT232H_CTRL_WE 	0x80		// Write Enable bit
#define FT232H_CTRL_RE 	0x40		// Read Enable bit
#define FT232H_CTRL_TXE 0x20		// When low, data can be written
#define FT232H_CTRL_RXF 0x10		// When low, there is data available

void ft232h_send_byte(uint8_t data);
uint8_t ft232h_read_byte();
uint8_t ft232h_read_bytes(uint32_t data_index, uint32_t data_lenght);
void ft232h_write_bytes(uint32_t data_index, uint32_t data_lenght);
void ft232h_send_string(char* string);

#endif
