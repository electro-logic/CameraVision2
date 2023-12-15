// Author: Leonardo Tazzini

#include "mipi_bridge_config.h"

//#include <stdio.h>		// printf (for debug only)
#include <unistd.h>		// usleep
#include <system.h>			// I2C_OPENCORES_MIPI_BASE
#include "I2C_core.h"		// OC_I2CL_Write, OC_I2CL_Read
#include "bit_helper.h"		// ReverseUInt16

// Clock notes
// Mipi Bridge REFCLK = 20 MHz (from FPGA). Max 40 MHz
//
// REFCLK is input clock of PLL. Output clock from PLL is pll_clk. PLL is controlled by PLLCtl0 and PLLCtl1
//
// pll_clk is then used to generate lower frequencies through divisors:
// 	PPIRxClk is controlled by PPIClkDiv (2/4/8 divisor)
// 	Sys_Clk, PClk are controlled by SClkDiv (2/4/8 divisor)
// 	MClk is controlled by MClkRefDiv (2/4/8 divisor)

// PPIRxClk is used in CSIRx for detecting CSI Link LP -> HS transition. Max 125 MHz, Min 66 MHz.
// PClk (Parallel Output Clock). Max 100MHz
// MClk is reference Clock to Sensor. Max 125 MHz

// Parallel Port Controller (pp_top) is controlled by sys_clk
// sys_clk is the same of PClk

// Video Buffer (vb_top) is controlled by wr_clk and rd_clk
// In CSI-2 RX to Parallel Port Operation line buffer is feed by sys_clk

// NOTES: MClk is reference Clock to Sensor.
// Sensor (OV8865) has internal PLL that accept 6-27 MHz Clock
// and generate by default internally 75 MHz pixel clock, 600 MHz MIPI serial clock and 120 MHz system clock.

// Pll_clk = RefClk* [(FBD + 1)/ (PRD + 1)] * [1 / (2^FRS)]
// PCLK = Pll_clk / SCLKDIV

#define WRITE_DELAY			0xFFFF

typedef struct{
	uint16_t Addr;
	uint16_t Data;
}MipiRegister;

const MipiRegister MIPI_REGISTERS[] = {
	// Reset state except all configuration registers content (regFile) and I2C slave module
	{REG_SysCtl, (1 << SysCtl_Sreset)},
	{WRITE_DELAY,10}, 					// delay
	{REG_SysCtl, (0 << SysCtl_Sreset)},
	{WRITE_DELAY,10},

	// Notes: Update PLL settings only when Mipi Bridge is in sleep mode (controller by reg_sleep)

	// REFCLK    20 MHz
	// PPIrxCLK  100 MHz
	// PCLK      50 MHz
	// MCLK      25 MHz

	{REG_SysCtl, (1 << SysCtl_SLEEP)},
	{REG_PLLCtl0,(0x01 << PLLCtl0_PLL_PRD) | (0x27 << PLLCtl0_PLL_FBD)},
	{REG_PLLCtl1,(0x01 << PLLCtl1_PLL_FRS) | (0x00 << PLLCtl1_PLL_LBWS) | (0x1 << PLLCtl1_PLL_RESETB) | (0x1 << PLLCtl1_PLL_EN) | (0x01 << PLLCtl1_PLL_LFBREN)},
	{WRITE_DELAY,10},
	{REG_PLLCtl1,(0x01 << PLLCtl1_PLL_FRS) | (0x00 << PLLCtl1_PLL_LBWS) | (0x1 << PLLCtl1_PLL_CKEN) | (0x1 << PLLCtl1_PLL_RESETB) | (0x1 << PLLCtl1_PLL_EN) | (0x01 << PLLCtl1_PLL_LFBREN)},
	{REG_SysCtl, (0 << SysCtl_SLEEP)},
	{WRITE_DELAY,10},

	{REG_CLKCtrl,(0x2 << CLKCtrl_PPIclkDiv) | (0x02 << CLKCtrl_MclkRefDiv) | (0x01 << CLKCtrl_SclkDiv)},
	{REG_MclkCtl,(0x1 << MclkCtl_mclk_high) | (0x1 << MclkCtl_mclk_low)},
	{REG_PHYTimDly,0x8006},		// MIPI PHY Time Delay Register (depends on PPIRXCLK)
	{REG_FiFoCtl, 0x64},
	{REG_DataFmt,(PDFormat_RAW10 << DataFmt_PDFormat) | (1 << DataFmt_UDT_en)},	// Output 10 bits RAW image
	{REG_ConfCtl, (1 << ConfCtl_TriEn) | (1 << ConfCtl_PPEn) | (1 << ConfCtl_Auto) | (0x00 << ConfCtl_DataLane)}	// | (1 << ConfCtl_PCLKP)
};

const int MIPI_REGISTERS_COUNT = sizeof(MIPI_REGISTERS) / sizeof(MipiRegister);

bool mipi_bridge_reg_write(uint16_t Addr, uint16_t Value)
{
	return OC_I2CL_Write(I2C_OPENCORES_MIPI_BASE, MIPI_BRIDGE_I2C_ADDR, Addr, (alt_u8 *)&Value, sizeof(Value));
}

uint16_t mipi_bridge_reg_read(uint16_t Addr)
{
	uint16_t value;
	OC_I2CL_Read(I2C_OPENCORES_MIPI_BASE,MIPI_BRIDGE_I2C_ADDR, Addr,(alt_u8 *)&value,sizeof(value));
	return ReverseUInt16(value);
}

bool mipi_bridge_init(void)
{
	/*
	// Debug only code
	uint16_t chipID = MIPI_REGISTERSRead(REG_ChipID); // read chip and revision id;
	if(chipID != MIPI_CHIP_REVISION_ID)
	{
		printf("TC358748XBG Chip and Revision ID: 0x%04xh don't match 0x4401\n", chipID);
		return;
	}
	*/
	for(int i = 0; i < MIPI_REGISTERS_COUNT; i++){
		if (MIPI_REGISTERS[i].Addr == WRITE_DELAY)
		{
			usleep(MIPI_REGISTERS[i].Data * 1000);
		}
		else
		{
			if(!mipi_bridge_reg_write(MIPI_REGISTERS[i].Addr, MIPI_REGISTERS[i].Data))
				return false;
		}
	}
	return true;
}
