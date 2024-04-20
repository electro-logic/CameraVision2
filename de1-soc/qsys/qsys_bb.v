
module qsys (
	clk_clk,
	clk_mipi_refclk_clk,
	clk_sdram_clk,
	i2c_opencores_camera_export_scl_pad_io,
	i2c_opencores_camera_export_sda_pad_io,
	i2c_opencores_mipi_export_scl_pad_io,
	i2c_opencores_mipi_export_sda_pad_io,
	led_external_connection_export,
	mipi_mipi_pixel_clk,
	mipi_mipi_pixel_d,
	mipi_mipi_pixel_hs,
	mipi_mipi_pixel_vs,
	mipi_pwdn_n_external_connection_export,
	mipi_reset_n_external_connection_export,
	reset_reset_n,
	sdram_wire_addr,
	sdram_wire_ba,
	sdram_wire_cas_n,
	sdram_wire_cke,
	sdram_wire_cs_n,
	sdram_wire_dq,
	sdram_wire_dqm,
	sdram_wire_ras_n,
	sdram_wire_we_n);	

	input		clk_clk;
	output		clk_mipi_refclk_clk;
	output		clk_sdram_clk;
	inout		i2c_opencores_camera_export_scl_pad_io;
	inout		i2c_opencores_camera_export_sda_pad_io;
	inout		i2c_opencores_mipi_export_scl_pad_io;
	inout		i2c_opencores_mipi_export_sda_pad_io;
	output	[7:0]	led_external_connection_export;
	input		mipi_mipi_pixel_clk;
	input	[9:0]	mipi_mipi_pixel_d;
	input		mipi_mipi_pixel_hs;
	input		mipi_mipi_pixel_vs;
	output		mipi_pwdn_n_external_connection_export;
	output		mipi_reset_n_external_connection_export;
	input		reset_reset_n;
	output	[12:0]	sdram_wire_addr;
	output	[1:0]	sdram_wire_ba;
	output		sdram_wire_cas_n;
	output		sdram_wire_cke;
	output		sdram_wire_cs_n;
	inout	[15:0]	sdram_wire_dq;
	output	[1:0]	sdram_wire_dqm;
	output		sdram_wire_ras_n;
	output		sdram_wire_we_n;
endmodule
