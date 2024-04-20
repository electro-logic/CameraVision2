create_clock -name CLOCK_50 -period "50.0 MHz" [get_ports {CLOCK_50} ]
derive_pll_clocks
derive_clock_uncertainty

# D8M
create_clock -name MIPI_PIXEL_CLK -period "50.0 MHz" [get_ports {MIPI_PIXEL_CLK} ]
create_clock -name MIPI_PIXEL_CLK_EXT -period "50.0 MHz"
set_input_delay -clock MIPI_PIXEL_CLK_EXT -max 6.1 [get_ports {MIPI_PIXEL_*} ]
set_input_delay -clock MIPI_PIXEL_CLK_EXT -min 0.9 [get_ports {MIPI_PIXEL_*} ]

# Clock groups
set_clock_groups -asynchronous \
-group [get_clocks {u0|pll_sys|altera_pll_i|general[0].gpll~PLL_OUTPUT_COUNTER|divclk}] \
-group [get_clocks {MIPI_PIXEL_CLK}]

# USB Blaster II
create_clock -name {altera_reserved_tck} -period "25.0 MHz" {altera_reserved_tck}
set_input_delay -clock altera_reserved_tck -clock_fall 3 [get_ports altera_reserved_tdi]
set_input_delay -clock altera_reserved_tck -clock_fall 3 [get_ports altera_reserved_tms]
set_output_delay -clock altera_reserved_tck 3 [get_ports altera_reserved_tdo]

#SDRAM
create_generated_clock -source [get_pins {u0|pll_sys|altera_pll_i|general[1].gpll~PLL_OUTPUT_COUNTER|divclk}] -name clk_dram_ext [get_ports {DRAM_CLK}]
set_output_delay -max -clock clk_dram_ext 1.6  [get_ports {DRAM_DQ* DRAM_*DQM}]
set_output_delay -min -clock clk_dram_ext -0.9 [get_ports {DRAM_DQ* DRAM_*DQM}]
set_output_delay -max -clock clk_dram_ext 1.6  [get_ports {DRAM_ADDR* DRAM_BA* DRAM_RAS_N DRAM_CAS_N DRAM_WE_N DRAM_CKE DRAM_CS_N}]
set_output_delay -min -clock clk_dram_ext -0.9 [get_ports {DRAM_ADDR* DRAM_BA* DRAM_RAS_N DRAM_CAS_N DRAM_WE_N DRAM_CKE DRAM_CS_N}]

# General
set_false_path -from [get_ports {KEY[*] LEDR[*]}] -to *
set_false_path -from * -to [get_ports {KEY[*] LEDR[*]}]
