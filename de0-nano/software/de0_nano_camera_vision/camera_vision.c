// Author: Leonardo Tazzini

// Camera Vision 2 - visit http://electro-logic.blogspot.it for info about this project
// NB: Some code in this project is taken or derived from Terasic D8M examples

// To bake the .elf into the bitstream right click on the project, Make Targets / Build / mem_init_generate

#include <io.h>			// IORD, IOWR
#include <system.h>		// I2C_OPENCORES_MIPI_BASE, MIPI_CONTROLLER_0_BASE
#include <stdio.h>		// printf
#include <stdint.h>		// uintX_t
#include <unistd.h>		// usleep

#include "I2C_core.h"
#include "mipi_bridge_config.h"
#include "mipi_camera_config.h"
#include "stdio_ext.h"
#include "ft232h.h"

#define I2C_SPEED		400000

// Camera Vision 2 Commands
#define CMD_RD_REG 		0x01
#define CMD_WR_REG 		0x02
#define CMD_RESET		0x04
#define CMD_WR_FOCUS	0x03
#define CMD_RD_FOCUS	0x07
#define CMD_CONFIG 		0x05
#define CMD_RD_IMG 		0x06
#define CMD_RD_COM		0x10
#define CMD_WR_COM		0x11
#define CMD_MIPI_RD_REG 0x08
#define CMD_MIPI_WR_REG 0x09

#define RESPONSE_OK	 	0xAA

#define COM_JTAG		0
#define COM_FT232H		1

// Setup MIPI Bridge and Camera
bool config()
{
	//See OV8865 pag. 30/34 for power down sequence details
	// MIPI_PWDN_N = OV8865 PWDNB
	// MIPI_RESET_N = TC358748XBG RESX
	IOWR(MIPI_PWDN_N_BASE, 0x00, 0x00);
	IOWR(MIPI_RESET_N_BASE, 0x00, 0x00);
	usleep(2 * 1000);
	IOWR(MIPI_PWDN_N_BASE, 0x00, 0xFF);
	usleep(2 * 1000);
	IOWR(MIPI_RESET_N_BASE, 0x00, 0xFF);
	usleep(2000);
	if(!oc_i2c_init_ex(I2C_OPENCORES_MIPI_BASE, ALT_CPU_FREQ, I2C_SPEED))
		return false;
	if(!mipi_bridge_init())
		return false;
	usleep(500*1000);
	if(!oc_i2c_init_ex(I2C_OPENCORES_CAMERA_BASE, ALT_CPU_FREQ, I2C_SPEED))
		return false;
	if(!mipi_camera_init())
		return false;
	usleep(1000);
	return true;
}
// Wait until frame is captured
void WaitFrame()
{
	uint32_t status = IORD(MIPI_BASE, 0x00);
	while((status  & 0x02)== 0x00)
	{
		usleep(1000 * 5);
		status = (IORD(MIPI_BASE, 0x00) & 0x02);
	}
}
int main()
{
	usleep(1000 * 100);
	if(!config())
	{
		IOWR(LED_BASE, 0x00, 0xFF);
		return 0;
	}

	// wait for one frame to adjust blc
	usleep(1000 * 500);

	IOWR(LED_BASE, 0x00, 0x01);
	uint16_t img_width = 3264;
	uint16_t img_height = 2448;

	uint16_t addr, mipiReg;
	uint8_t reg;
	byte cmd, com;
	while(1){
		cmd = readByte();
		switch(cmd)
		{
			case CMD_RD_COM:
				writeByte(com);
				writeByte(RESPONSE_OK);
				break;
			case CMD_WR_COM:
				com = readByte();
				writeByte(RESPONSE_OK);
				break;
			case CMD_RD_REG:
				addr = readUInt16();
				reg = mipi_camera_reg_read(addr);
				writeByte(reg);
				break;
			case CMD_WR_REG:
				addr = readUInt16();
				reg = readByte();
				mipi_camera_reg_write(addr, reg);
				break;
			case CMD_MIPI_RD_REG:
				addr = readUInt16();
				mipiReg = mipi_bridge_reg_read(addr);
				writeUInt16(mipiReg);
				break;
			case CMD_MIPI_WR_REG:
				addr = readUInt16();
				mipiReg = readUInt16();
				mipi_bridge_reg_write(addr, mipiReg);
				break;
			case CMD_WR_FOCUS:
				addr = readUInt16();
				mipi_camera_reg_write_VCM149C(addr);
				writeByte(RESPONSE_OK);
				break;
			case CMD_RD_FOCUS:
				addr = mipi_camera_reg_read_VCM149C();
				writeBytes((byte*)&addr, 2);
				writeByte(RESPONSE_OK);
				break;
			case CMD_RESET:
				config();
				writeByte(RESPONSE_OK);
				break;
			case CMD_CONFIG:
				img_width = readUInt16();
				img_height = readUInt16();
				writeByte(RESPONSE_OK);
				break;
			case CMD_RD_IMG:
				IOWR(LED_BASE, 0x00, 0x00);
				IOWR(MIPI_BASE, 0x00, 0x00000001);	// Stop capture request
				WaitFrame();
				switch(com)
				{
				case COM_JTAG:
					// send row by row
					for(int rowIndex=0;rowIndex<img_height;rowIndex++)
					{
						byte *bytes = (byte *)(SDRAM_BASE + (rowIndex * img_width * 2));
						writeBytes(bytes, img_width * 2);
					}
					break;
				case COM_FT232H:
					// send the whole frame
					ft232h_write_bytes(0, img_width*img_height*2);
					break;
				}
				IOWR(MIPI_BASE, 0x00, 0x00000000);	// Start capture request
				IOWR(LED_BASE, 0x00, 0x01);
				writeByte(RESPONSE_OK);
				break;
		}
	};
	return 0;
}
