// Author: Leonardo Tazzini

// MIPI Bridge is TC358748XBG chip from Toshiba. Convert CSI-2 signal from CMOS camera into Parallel for FPGA utilization.

#ifndef MIPI_BRIDGE_CONFIG_H_
#define MIPI_BRIDGE_CONFIG_H_

#include <stdint.h>		// uintX_t
#include "stdbool.h"

// Config TC358748XBG to work in CSI-2 RX to Parallel Port Operation
bool mipi_bridge_init();
// Write internal TC358748 register
bool mipi_bridge_reg_write(uint16_t Addr, uint16_t Value);
// Read internal TC358748 register
uint16_t mipi_bridge_reg_read(uint16_t Addr);

#define MIPI_BRIDGE_I2C_ADDR	0x1C
#define MIPI_CHIP_REVISION_ID	0x4401

#define REG_ChipID 			0x0000	// TC358746AXBG/TC358748XBG Chip and Revision ID

#define REG_SysCtl 			0x0002	// System Control Register
#define SysCtl_Sreset		0x0000
#define SysCtl_SLEEP		0x0001

// REG_ConfCtl notes:
// 0x8047	data normal (on clock rising), terasic default
// 0x804F	data inverted (on clock falling)
#define REG_ConfCtl			0x0004	// Configuration Control Register
#define ConfCtl_DataLane	0x0000	// CSI-2 Data Lane Select (2 bit). Selects the number data lane activated during data transfer (1 to 4)
#define ConfCtl_Auto		0x0002	// I2C slave index increment
#define ConfCtl_PCLKP		0x0003	// Parallel Clock (PCLK) Polarity Select
#define ConfCtl_PPEn		0x0006	// Parallel Port Enable
#define ConfCtl_TriEn		0x000F	// Parallel Out (MSEL = 0) and CS = 1, Tri-State Enable

#define REG_FiFoCtl			0x0006	// FiFo Control Register

#define REG_DataFmt			0x0008	// Data Format Control Register
#define DataFmt_UDT_en		0x0000	// User Data Type ID enable
#define DataFmt_PDFormat	0x0004	// Peripheral Data Format (4 bit)
#define PDFormat_RAW8		0x00
#define PDFormat_RAW10		0x01
#define PDFormat_RAW12		0x02
#define PDFormat_RAW14		0x08

#define REG_MclkCtl			0x000C	// Mclk control register
#define MclkCtl_mclk_low	0x0000	// Mclk LOW time count
#define MclkCtl_mclk_high	0x0008	// Mclk HIGH time count
// NOTES: Total MClk divider = (mclk_high + 1) + (mclk_low + 1)

#define REG_PLLCtl0			0x0016	// PLL control Register 0
#define PLLCtl0_PLL_FBD		0x0000	// Feedback divider setting. Division ratio = (FBD8..0) + 1
#define PLLCtl0_PLL_PRD		0x000C	// Input divider setting. Division ratio = (PRD3..0) + 1

#define REG_PLLCtl1			0x0018	// PLL control Register 1
#define PLLCtl1_PLL_FRS		0x000A	// Frequency range setting (post divider) for HSCK frequency
#define PLLCtl1_PLL_LBWS	0x0008	// Loop bandwidth setting
#define PLLCtl1_PLL_LFBREN	0x0006	// Lower Frequency Bound Removal Enable
#define PLLCtl1_PLL_BYPCKEN	0x0005	// Bypass clock enable
#define PLLCtl1_PLL_CKEN	0x0004	// Clock enable
#define PLLCtl1_PLL_RESETB	0x0001	// PLL Reset
#define PLLCtl1_PLL_EN		0x0000	// PLL Enable

// Notes for REG_CLKCtrl divider selection:
// 0x00 : Pll_clk DIV 8
// 0x01 : Pll_clk DIV 4
// 0x02 : Pll_clk DIV 2

#define REG_CLKCtrl			0x0020	// Clock Control Register
#define CLKCtrl_SclkDiv		0x0000	// Sys_clk Output Divider Selection (same as parallel output clock, PClk)
#define CLKCtrl_MclkRefDiv	0x0002	// MclkRef Output Divider Selection
#define CLKCtrl_PPIclkDiv	0x0004	// PPI Output Divider Selection

#define REG_WordCnt			0x0022	// Word Count Register
#define REG_PHYTimDly		0x0060	// CSI2RXPHY Time delay Register

#endif
