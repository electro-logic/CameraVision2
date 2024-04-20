create_clock -name CLOCK_50 -period "50.0 MHz" [get_ports {CLOCK_50} ]
create_clock -period 20 [get_ports CLOCK2_50]
create_clock -period 20 [get_ports CLOCK3_50]
create_clock -period 20 [get_ports CLOCK4_50]
derive_pll_clocks
derive_clock_uncertainty

## FT232H
#create_clock -name USB_CLK -period "60.0 MHz" [get_ports {FT232H_CLK}]
#create_clock -name USB_CLK_EXT -period "60.0 MHz"
#set_output_delay -max -clock USB_CLK_EXT 7.5 [get_ports {FT232H_ADBUS[*]}]
#set_output_delay -max -clock USB_CLK_EXT 7.5 [get_ports {FT232H_nWR}]
#set_output_delay -max -clock USB_CLK_EXT 7.5 [get_ports {FT232H_nOE}]
#set_output_delay -max -clock USB_CLK_EXT 7.5 [get_ports {FT232H_nRD}]
##set_input_delay -max -clock USB_CLK_EXT 9 [get_ports {FT232H_nTXE}]
##set_input_delay -max -clock USB_CLK_EXT 9 [get_ports {FT232H_nRXF}]
#set_input_delay -max -clock USB_CLK_EXT 7.5 [get_ports {FT232H_ADBUS[*]}]
#set_output_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_ADBUS[*]}]
#set_output_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_nWR}]
#set_output_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_nOE}]
#set_output_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_nRD}]
##set_input_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_nTXE}]
##set_input_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_nRXF}]
#set_input_delay -min -clock USB_CLK_EXT 0 [get_ports {FT232H_ADBUS[*]}]

# D8M
create_clock -name MIPI_PIXEL_CLK -period "50.0 MHz" [get_ports {MIPI_PIXEL_CLK} ]
create_clock -name MIPI_PIXEL_CLK_EXT -period "50.0 MHz"
set_input_delay -clock MIPI_PIXEL_CLK_EXT -max 6 [get_ports {MIPI_PIXEL_*} ]
set_input_delay -clock MIPI_PIXEL_CLK_EXT -min 1 [get_ports {MIPI_PIXEL_*} ]

# SDRAM
#create_generated_clock -source [get_pins {u0|pll_sys|sd1|pll7|clk[1]}] -name clk_dram_ext [get_ports {DRAM_CLK}]
#set_input_delay -max -clock clk_dram_ext 5.9 [get_ports DRAM_DQ*]
#set_input_delay -min -clock clk_dram_ext 3.0 [get_ports DRAM_DQ*]
#set_output_delay -max -clock clk_dram_ext 1.6  [get_ports {DRAM_DQ* DRAM_*DQM}]
#set_output_delay -min -clock clk_dram_ext -0.9 [get_ports {DRAM_DQ* DRAM_*DQM}]
#set_output_delay -max -clock clk_dram_ext 1.6  [get_ports {DRAM_ADDR* DRAM_BA* DRAM_RAS_N DRAM_CAS_N DRAM_WE_N DRAM_CKE DRAM_CS_N}]
#set_output_delay -min -clock clk_dram_ext -0.9 [get_ports {DRAM_ADDR* DRAM_BA* DRAM_RAS_N DRAM_CAS_N DRAM_WE_N DRAM_CKE DRAM_CS_N}]
#set_multicycle_path -from [get_clocks {clk_dram_ext}] -to [get_clocks {u0|pll_sys|sd1|pll7|clk[0]}] -setup 2

# Clock groups
#set_clock_groups -asynchronous \
#-group [get_clocks {MIPI_PIXEL_CLK}] \
#-group { USB_CLK_EXT u0|pll_debug|sd1|pll7|clk[0]}

# USB Blaster II
create_clock -name {altera_reserved_tck} -period "25.0 MHz" {altera_reserved_tck}
set_input_delay -clock altera_reserved_tck -clock_fall 3 [get_ports altera_reserved_tdi]
set_input_delay -clock altera_reserved_tck -clock_fall 3 [get_ports altera_reserved_tms]
set_output_delay -clock altera_reserved_tck 3 [get_ports altera_reserved_tdo]

# General
set_false_path -from [get_ports {KEY[*] LEDR[*]}] -to *
set_false_path -from * -to [get_ports {KEY[*] LEDR[*]}]

#create_clock -period "143 MHz" -name clk_dram [get_ports DRAM_CLK]
#
##**************************************************************
## Set Input Delay
##**************************************************************
## Board Delay (Data) + Propagation Delay - Board Delay (Clock)
#set_input_delay -max -clock clk_dram -0.048 [get_ports DRAM_DQ*]
#set_input_delay -min -clock clk_dram -0.057 [get_ports DRAM_DQ*]
#
##**************************************************************
## Set Output Delay
##**************************************************************
## max : Board Delay (Data) - Board Delay (Clock) + tsu (External Device)
## min : Board Delay (Data) - Board Delay (Clock) - th (External Device)
#set_output_delay -max -clock clk_dram 1.452  [get_ports DRAM_DQ*]
#set_output_delay -min -clock clk_dram -0.857 [get_ports DRAM_DQ*]
#set_output_delay -max -clock clk_dram 1.531 [get_ports DRAM_ADDR*]
#set_output_delay -min -clock clk_dram -0.805 [get_ports DRAM_ADDR*]
#set_output_delay -max -clock clk_dram 1.533  [get_ports DRAM_*DQM]
#set_output_delay -min -clock clk_dram -0.805 [get_ports DRAM_*DQM]
#set_output_delay -max -clock clk_dram 1.510  [get_ports DRAM_BA*]
#set_output_delay -min -clock clk_dram -0.800 [get_ports DRAM_BA*]
#set_output_delay -max -clock clk_dram 1.520  [get_ports DRAM_RAS_N]
#set_output_delay -min -clock clk_dram -0.780 [get_ports DRAM_RAS_N]
#set_output_delay -max -clock clk_dram 1.5000  [get_ports DRAM_CAS_N]
#set_output_delay -min -clock clk_dram -0.800 [get_ports DRAM_CAS_N]
#set_output_delay -max -clock clk_dram 1.545 [get_ports DRAM_WE_N]
#set_output_delay -min -clock clk_dram -0.755 [get_ports DRAM_WE_N]
#set_output_delay -max -clock clk_dram 1.496  [get_ports DRAM_CKE]
#set_output_delay -min -clock clk_dram -0.804 [get_ports DRAM_CKE]
#set_output_delay -max -clock clk_dram 1.508  [get_ports DRAM_CS_N]
#set_output_delay -min -clock clk_dram -0.792 [get_ports DRAM_CS_N]









