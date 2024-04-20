	qsys u0 (
		.clk_clk                                 (<connected-to-clk_clk>),                                 //                              clk.clk
		.clk_mipi_refclk_clk                     (<connected-to-clk_mipi_refclk_clk>),                     //                  clk_mipi_refclk.clk
		.clk_sdram_clk                           (<connected-to-clk_sdram_clk>),                           //                        clk_sdram.clk
		.i2c_opencores_camera_export_scl_pad_io  (<connected-to-i2c_opencores_camera_export_scl_pad_io>),  //      i2c_opencores_camera_export.scl_pad_io
		.i2c_opencores_camera_export_sda_pad_io  (<connected-to-i2c_opencores_camera_export_sda_pad_io>),  //                                 .sda_pad_io
		.i2c_opencores_mipi_export_scl_pad_io    (<connected-to-i2c_opencores_mipi_export_scl_pad_io>),    //        i2c_opencores_mipi_export.scl_pad_io
		.i2c_opencores_mipi_export_sda_pad_io    (<connected-to-i2c_opencores_mipi_export_sda_pad_io>),    //                                 .sda_pad_io
		.led_external_connection_export          (<connected-to-led_external_connection_export>),          //          led_external_connection.export
		.mipi_mipi_pixel_clk                     (<connected-to-mipi_mipi_pixel_clk>),                     //                             mipi.mipi_pixel_clk
		.mipi_mipi_pixel_d                       (<connected-to-mipi_mipi_pixel_d>),                       //                                 .mipi_pixel_d
		.mipi_mipi_pixel_hs                      (<connected-to-mipi_mipi_pixel_hs>),                      //                                 .mipi_pixel_hs
		.mipi_mipi_pixel_vs                      (<connected-to-mipi_mipi_pixel_vs>),                      //                                 .mipi_pixel_vs
		.mipi_pwdn_n_external_connection_export  (<connected-to-mipi_pwdn_n_external_connection_export>),  //  mipi_pwdn_n_external_connection.export
		.mipi_reset_n_external_connection_export (<connected-to-mipi_reset_n_external_connection_export>), // mipi_reset_n_external_connection.export
		.reset_reset_n                           (<connected-to-reset_reset_n>),                           //                            reset.reset_n
		.sdram_wire_addr                         (<connected-to-sdram_wire_addr>),                         //                       sdram_wire.addr
		.sdram_wire_ba                           (<connected-to-sdram_wire_ba>),                           //                                 .ba
		.sdram_wire_cas_n                        (<connected-to-sdram_wire_cas_n>),                        //                                 .cas_n
		.sdram_wire_cke                          (<connected-to-sdram_wire_cke>),                          //                                 .cke
		.sdram_wire_cs_n                         (<connected-to-sdram_wire_cs_n>),                         //                                 .cs_n
		.sdram_wire_dq                           (<connected-to-sdram_wire_dq>),                           //                                 .dq
		.sdram_wire_dqm                          (<connected-to-sdram_wire_dqm>),                          //                                 .dqm
		.sdram_wire_ras_n                        (<connected-to-sdram_wire_ras_n>),                        //                                 .ras_n
		.sdram_wire_we_n                         (<connected-to-sdram_wire_we_n>)                          //                                 .we_n
	);

