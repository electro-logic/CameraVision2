#include "ft232h.h"

void ft232h_send_byte(uint8_t data)
{
	// Wait that nTXE is low (data can be written)
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_TXE)){}

	// Set Data to Write
	IOWR_8DIRECT(FT232H_BASE, FT232H_DATA_REG, data);

	// Set Write Enable
	IOWR_8DIRECT(FT232H_BASE, FT232H_CTRL_REG, FT232H_CTRL_WE);

	// Wait that Write is complete
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_WE)){}
}

uint8_t ft232h_read_byte()
{
	// Wait that nRXF is low (there is data available)
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_RXF)){}

	// Set Read Enable
	IOWR_8DIRECT(FT232H_BASE, FT232H_CTRL_REG, FT232H_CTRL_RE);

	// Wait that Read is complete
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_RE)){}

	// Read Result
	uint8_t data = IORD_8DIRECT(FT232H_BASE, FT232H_DATA_REG);
	return data;
}

uint8_t ft232h_read_bytes(uint32_t data_index, uint32_t data_lenght)
{
	// Wait that nRXF is low (there is data available)
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_RXF)){}

	// Set Data Index
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI0_REG, data_index & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI1_REG, (data_index >> 8) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI2_REG, (data_index >> 16) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI3_REG, (data_index >> 24) & 0xFF);

	// Set Data Length
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL0_REG, data_lenght & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL1_REG, (data_lenght >> 8) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL2_REG, (data_lenght >> 16) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL3_REG, (data_lenght >> 24) & 0xFF);

	// Set Read Enable
	IOWR_8DIRECT(FT232H_BASE, FT232H_CTRL_REG, FT232H_CTRL_RE);

	// Wait that Read is complete
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_RE)){}

	// Read Result
	uint8_t data = IORD_8DIRECT(FT232H_BASE, FT232H_DATA_REG);
	return data;
}

void ft232h_write_bytes(uint32_t data_index, uint32_t data_lenght)
{
	// Wait that nTXE is low (data can be written)
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_TXE)){}

	// Set Data Index
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI0_REG, data_index & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI1_REG, (data_index >> 8) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI2_REG, (data_index >> 16) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DI3_REG, (data_index >> 24) & 0xFF);

	// Set Data Length
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL0_REG, data_lenght & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL1_REG, (data_lenght >> 8) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL2_REG, (data_lenght >> 16) & 0xFF);
	IOWR_8DIRECT(FT232H_BASE, FT232H_DL3_REG, (data_lenght >> 24) & 0xFF);

	// Set Write Enable
	IOWR_8DIRECT(FT232H_BASE, FT232H_CTRL_REG, FT232H_CTRL_WE);

	// Wait that Write is complete
	while((IORD_8DIRECT(FT232H_BASE, FT232H_CTRL_REG) & FT232H_CTRL_WE)){}

}

void ft232h_send_string(char* string)
{
	while(*string)
	{
		ft232h_send_byte(*string);
		string++;
	}
}
