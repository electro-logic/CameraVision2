// Author: Leonardo Tazzini

// MIPI Camera is OV8865 cmos camera chip from Omnivision.
// VCM149C is 120mA 10bit current sinking VCM driver, drive focus motor.

// Documentation:
// https://git.kernel.org/pub/scm/linux/kernel/git/stable/linux.git/tree/drivers/media/i2c//ov8865.c?h=master

#ifndef MIPI_CAMERA_CONFIG_H_
#define MIPI_CAMERA_CONFIG_H_

#include <stdint.h>		// uintX_t
#include "stdbool.h"

#define MIPI_I2C_ADDR   	0x6C
#define MIPI_AF_I2C_ADDR	0x18

bool 	mipi_camera_init();
void	mipi_camera_binning(uint8_t level);
uint8_t mipi_camera_reg_read(uint16_t Addr);
bool 	mipi_camera_reg_write(uint16_t Addr, uint8_t Value);

bool	mipi_camera_reg_write_VCM149C(uint16_t dacValue);
uint16_t mipi_camera_reg_read_VCM149C();


// Note: Horizontal blanking = HTS - Horizontal output width
// HTS: Horizontal total size
// VTS: Vertical total size
// fps = SCLK / (HTS * VTS)

// MipiClk is for the MIPI and SysClk is for the internal clock of the Image Signal Processing (ISP) block

// As the camera mipi lvds data is processed by MIPI parallel Bridge IC, it will be limited by mipi parallel internal Linebuffer length,
// then, customer can try to set HTS value as larger as possible ( on condition that VTS value is still meet your requirement)
// to make a longer H blanking to reduce peak bandwidth requirements.

#define OV8865_SC_CTRL0100	0x0100	// software_standby
#define OV8865_SC_CTRL0103	0x0103	// software_reset

#define OV8865_BLC_NUM_OPTION	0x3830	// ablc_adj, ablc_use_num
#define OV8865_ZLINE_NUM_OPTION	0x3836	// zline_use_num

// PLL Registers
#define OV8865_PLL_CTRL_0	0x0300	// pll1_pre_div
#define OV8865_PLL_CTRL_1	0x0301	// pll1_multiplier[9:8]
#define OV8865_PLL_CTRL_2	0x0302	// pll1_multiplier[7:0]
#define OV8865_PLL_CTRL_3	0x0303	// pll1_divm
#define OV8865_PLL_CTRL_4	0x0304	// pll1_div_mipi
#define OV8865_PLL_CTRL_5	0x0305	// pll1_div_sp
#define OV8865_PLL_CTRL_6	0x0306	// pll1_div_s
#define OV8865_PLL_CTRL_8	0x0308	// pll1_bypass
#define OV8865_PLL_CTRL_9	0x0309	// pll1_cp
#define OV8865_PLL_CTRL_A	0x030A	// pll1_predivp
#define OV8865_PLL_CTRL_B	0x030B	// pll2_pre_div
#define OV8865_PLL_CTRL_C	0x030C	// pll2_r_divp[9:8]
#define OV8865_PLL_CTRL_D	0x030D	// pll2_r_divp[7:0]
#define OV8865_PLL_CTRL_E	0x030E	// pll2_r_divs
#define OV8865_PLL_CTRL_F	0x030F	// pll2_r_divsp
#define OV8865_PLL_CTRL_10	0x0310	// pll2_r_cp
#define OV8865_PLL_CTRL_11	0x0311	// pll2_bypass
#define OV8865_PLL_CTRL_12	0x0312	// pll2_pre_div0, pll2_r_divdac

// VCO range: 500 - 1200 MHz

// MIPI control registers
#define OV8865_MIPI_PCLK_PERIOD 0x4837
#define OV8865_MIPI_LP_GPIO0    0x4838
#define OV8865_MIPI_LP_GPIO1    0x4839
#define OV8865_MIPI_LANE_SEL01	0x4850
#define OV8865_MIPI_LANE_SEL23	0x4851

#define OV8865_MIPI_SC_CTRL0	0x3018
#define OV8865_MIPI_CTRL0       0x4800

// VarioPixel
#define OV8865_VAP_CTRL0		0x5900
#define OV8865_VAP_CTRL1		0x5901

#endif
