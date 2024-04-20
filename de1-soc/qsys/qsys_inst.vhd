	component qsys is
		port (
			clk_clk                                 : in    std_logic                     := 'X';             -- clk
			clk_mipi_refclk_clk                     : out   std_logic;                                        -- clk
			clk_sdram_clk                           : out   std_logic;                                        -- clk
			i2c_opencores_camera_export_scl_pad_io  : inout std_logic                     := 'X';             -- scl_pad_io
			i2c_opencores_camera_export_sda_pad_io  : inout std_logic                     := 'X';             -- sda_pad_io
			i2c_opencores_mipi_export_scl_pad_io    : inout std_logic                     := 'X';             -- scl_pad_io
			i2c_opencores_mipi_export_sda_pad_io    : inout std_logic                     := 'X';             -- sda_pad_io
			led_external_connection_export          : out   std_logic_vector(7 downto 0);                     -- export
			mipi_mipi_pixel_clk                     : in    std_logic                     := 'X';             -- mipi_pixel_clk
			mipi_mipi_pixel_d                       : in    std_logic_vector(9 downto 0)  := (others => 'X'); -- mipi_pixel_d
			mipi_mipi_pixel_hs                      : in    std_logic                     := 'X';             -- mipi_pixel_hs
			mipi_mipi_pixel_vs                      : in    std_logic                     := 'X';             -- mipi_pixel_vs
			mipi_pwdn_n_external_connection_export  : out   std_logic;                                        -- export
			mipi_reset_n_external_connection_export : out   std_logic;                                        -- export
			reset_reset_n                           : in    std_logic                     := 'X';             -- reset_n
			sdram_wire_addr                         : out   std_logic_vector(12 downto 0);                    -- addr
			sdram_wire_ba                           : out   std_logic_vector(1 downto 0);                     -- ba
			sdram_wire_cas_n                        : out   std_logic;                                        -- cas_n
			sdram_wire_cke                          : out   std_logic;                                        -- cke
			sdram_wire_cs_n                         : out   std_logic;                                        -- cs_n
			sdram_wire_dq                           : inout std_logic_vector(15 downto 0) := (others => 'X'); -- dq
			sdram_wire_dqm                          : out   std_logic_vector(1 downto 0);                     -- dqm
			sdram_wire_ras_n                        : out   std_logic;                                        -- ras_n
			sdram_wire_we_n                         : out   std_logic                                         -- we_n
		);
	end component qsys;

	u0 : component qsys
		port map (
			clk_clk                                 => CONNECTED_TO_clk_clk,                                 --                              clk.clk
			clk_mipi_refclk_clk                     => CONNECTED_TO_clk_mipi_refclk_clk,                     --                  clk_mipi_refclk.clk
			clk_sdram_clk                           => CONNECTED_TO_clk_sdram_clk,                           --                        clk_sdram.clk
			i2c_opencores_camera_export_scl_pad_io  => CONNECTED_TO_i2c_opencores_camera_export_scl_pad_io,  --      i2c_opencores_camera_export.scl_pad_io
			i2c_opencores_camera_export_sda_pad_io  => CONNECTED_TO_i2c_opencores_camera_export_sda_pad_io,  --                                 .sda_pad_io
			i2c_opencores_mipi_export_scl_pad_io    => CONNECTED_TO_i2c_opencores_mipi_export_scl_pad_io,    --        i2c_opencores_mipi_export.scl_pad_io
			i2c_opencores_mipi_export_sda_pad_io    => CONNECTED_TO_i2c_opencores_mipi_export_sda_pad_io,    --                                 .sda_pad_io
			led_external_connection_export          => CONNECTED_TO_led_external_connection_export,          --          led_external_connection.export
			mipi_mipi_pixel_clk                     => CONNECTED_TO_mipi_mipi_pixel_clk,                     --                             mipi.mipi_pixel_clk
			mipi_mipi_pixel_d                       => CONNECTED_TO_mipi_mipi_pixel_d,                       --                                 .mipi_pixel_d
			mipi_mipi_pixel_hs                      => CONNECTED_TO_mipi_mipi_pixel_hs,                      --                                 .mipi_pixel_hs
			mipi_mipi_pixel_vs                      => CONNECTED_TO_mipi_mipi_pixel_vs,                      --                                 .mipi_pixel_vs
			mipi_pwdn_n_external_connection_export  => CONNECTED_TO_mipi_pwdn_n_external_connection_export,  --  mipi_pwdn_n_external_connection.export
			mipi_reset_n_external_connection_export => CONNECTED_TO_mipi_reset_n_external_connection_export, -- mipi_reset_n_external_connection.export
			reset_reset_n                           => CONNECTED_TO_reset_reset_n,                           --                            reset.reset_n
			sdram_wire_addr                         => CONNECTED_TO_sdram_wire_addr,                         --                       sdram_wire.addr
			sdram_wire_ba                           => CONNECTED_TO_sdram_wire_ba,                           --                                 .ba
			sdram_wire_cas_n                        => CONNECTED_TO_sdram_wire_cas_n,                        --                                 .cas_n
			sdram_wire_cke                          => CONNECTED_TO_sdram_wire_cke,                          --                                 .cke
			sdram_wire_cs_n                         => CONNECTED_TO_sdram_wire_cs_n,                         --                                 .cs_n
			sdram_wire_dq                           => CONNECTED_TO_sdram_wire_dq,                           --                                 .dq
			sdram_wire_dqm                          => CONNECTED_TO_sdram_wire_dqm,                          --                                 .dqm
			sdram_wire_ras_n                        => CONNECTED_TO_sdram_wire_ras_n,                        --                                 .ras_n
			sdram_wire_we_n                         => CONNECTED_TO_sdram_wire_we_n                          --                                 .we_n
		);

