-- Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

entity de0_nano_d8m is

	port 
	(
		-- CLOCK
		CLOCK_50				: in std_logic;
		
		-- D8M-GPIO
		CAMERA_I2C_SCL		: inout std_logic;
		CAMERA_I2C_SDA		: inout std_logic;
		CAMERA_PWDN_n		: out std_logic;								-- Power Down signal of MIPI camera
		MIPI_PIXEL_CLK		: in std_logic;								-- Parallel Port Clock signal
		MIPI_PIXEL_D		: in std_logic_vector(9 downto 0);		-- Parallel Port Data
		MIPI_PIXEL_HS		: in std_logic;								-- Parallel Port Horizontal Synchronization signal
		MIPI_PIXEL_VS		: in std_logic;								-- Parallel Port Vertical Synchronization signal
		MIPI_CS_n			: out std_logic;								-- Mipi decoder Chip Select (active low)
		MIPI_MCLK			: out std_logic;								-- Reserved, not used		
		MIPI_REFCLK			: out std_logic;								-- Reference Clock Input of bridge device
		MIPI_RESET_n		: out std_logic;								-- Master Reset signal for MIPI camera and bridge device
		MIPI_I2C_SCL		: inout std_logic;							-- I2C Clock for bridge device
		MIPI_I2C_SDA		: inout std_logic;							-- I2C Data for bridge device
		
		-- FT232H
		FT232H_CLK			: in std_logic;								-- FT232H 60 MHz Clock
		FT232H_nTXE			: in std_logic;                   		-- FT232H Output Enable
		FT232H_nRXF			: in std_logic;                   		-- FT232H Output Enable
		FT232H_nOE			: out std_logic;                  		-- FT232H Output Enable		
		FT232H_nRD			: out std_logic;                  		-- FT232H Output Enable
		FT232H_nWR			: out std_logic;                  		-- FT232H Output Enable
		FT232H_nSIWU		: out std_logic;                  		-- FT232H Output Enable
		FT232H_ADBUS		: inout std_logic_vector(7 downto 0);	-- FT232H Output Enable
		
		-- SDRAM
		DRAM_ADDR			: out std_logic_vector(12 downto 0);
		DRAM_BA				: out std_logic_vector(1 downto 0);
		DRAM_CAS_N			: out std_logic;		
		DRAM_CKE				: out std_logic;
		DRAM_CLK				: out std_logic;
		DRAM_CS_N			: out std_logic;
		DRAM_DQ				: inout std_logic_vector(15 downto 0);
		DRAM_DQM				: out std_logic_vector(1 downto 0);
		DRAM_RAS_N			: out std_logic;				
		DRAM_WE_N			: out std_logic;

		-- KEY
		KEY					: in std_logic_vector(1 downto 0);
	
		-- LED
		LED					: out std_logic_vector(7 downto 0)
	);

end entity;

architecture rtl of de0_nano_d8m is

    component qsys is
		port (
			clk_clk                                 : in    std_logic                     := '0';
			clk_mipi_refclk_clk                     : out   std_logic;                                        
         clk_sdram_clk                           : out   std_logic;                                        						
			ft232h_nOE                              : out   std_logic;                                        
			ft232h_nTXE                             : in    std_logic                     := '0';             
			ft232h_nRXF                             : in    std_logic                     := '0';             
			ft232h_nRD                              : out   std_logic;                                        
			ft232h_nWR                              : out   std_logic;                                        
			ft232h_nSIWU                            : out   std_logic;                                        
			ft232h_ADBUS                            : inout std_logic_vector(7 downto 0)  := (others => '0'); 
			ft232h_clk_clk                          : in    std_logic                     := '0';             			
			i2c_opencores_camera_export_scl_pad_io  : inout std_logic                     := '0';             
			i2c_opencores_camera_export_sda_pad_io  : inout std_logic                     := '0';             
			i2c_opencores_mipi_export_scl_pad_io    : inout std_logic                     := '0';             
			i2c_opencores_mipi_export_sda_pad_io    : inout std_logic                     := '0';             
			mipi_pwdn_n_external_connection_export  : out   std_logic;                                        
			mipi_reset_n_external_connection_export : out   std_logic;                                        
			reset_reset_n                           : in    std_logic                     := '0';             			
			mipi_mipi_pixel_clk                     : in    std_logic                     := 'X';             
			mipi_mipi_pixel_d                       : in    std_logic_vector(9 downto 0)  := (others => 'X'); 
			mipi_mipi_pixel_hs                      : in    std_logic                     := 'X';             
			mipi_mipi_pixel_vs                      : in    std_logic                     := 'X';             
			sdram_wire_addr                         : out   std_logic_vector(12 downto 0);                    
			sdram_wire_ba                           : out   std_logic_vector(1 downto 0);                     
			sdram_wire_cas_n                        : out   std_logic;                                        
			sdram_wire_cke                          : out   std_logic;                                        
			sdram_wire_cs_n                         : out   std_logic;                                        
			sdram_wire_dq                           : inout std_logic_vector(15 downto 0) := (others => 'X'); 
			sdram_wire_dqm                          : out   std_logic_vector(1 downto 0);                     
			sdram_wire_ras_n                        : out   std_logic;                                        
			sdram_wire_we_n                         : out   std_logic;
			led_external_connection_export          : out   std_logic_vector(7 downto 0)
		);
    end component qsys;			
		
	component debug_pll IS
	PORT
	(
		areset		: IN STD_LOGIC  := '0';
		inclk0		: IN STD_LOGIC  := '0';
		c0				: OUT STD_LOGIC 
	);
	END component debug_pll;
		
	attribute noprune: boolean;
	attribute preserve: boolean;	
	signal DEBUG_CLOCK : STD_LOGIC; attribute noprune of DEBUG_CLOCK : signal is true; attribute preserve of DEBUG_CLOCK: signal is true;
begin	

	-- MIPI_CS_n = 0, normal operation,
	-- MIPI_CS_n = 1, chip not selected. Cannot access to internal registers and optionally Parallel output ports can be tri-state when 0x0004[15] is set
	MIPI_CS_n 		<= '0';	
	
	u0 : component qsys port map (
		clk_clk       										=> CLOCK_50,
		reset_reset_n 										=> KEY(0),
		clk_mipi_refclk_clk                    	=> MIPI_REFCLK,
		i2c_opencores_mipi_export_scl_pad_io    	=> MIPI_I2C_SCL, 
		i2c_opencores_mipi_export_sda_pad_io    	=> MIPI_I2C_SDA,		
		mipi_reset_n_external_connection_export 	=> MIPI_RESET_n,				
		i2c_opencores_camera_export_scl_pad_io	 	=> CAMERA_I2C_SCL,
		i2c_opencores_camera_export_sda_pad_io		=> CAMERA_I2C_SDA,
      mipi_pwdn_n_external_connection_export		=> CAMERA_PWDN_n,		
		mipi_mipi_pixel_clk								=> MIPI_PIXEL_CLK,
		mipi_mipi_pixel_d									=> MIPI_PIXEL_D,
		mipi_mipi_pixel_hs								=> MIPI_PIXEL_HS,
		mipi_mipi_pixel_vs								=> MIPI_PIXEL_VS,
		clk_sdram_clk										=> DRAM_CLK,
		sdram_wire_addr									=> DRAM_ADDR,
		sdram_wire_ba										=> DRAM_BA,
		sdram_wire_cas_n									=> DRAM_CAS_N,
		sdram_wire_cke										=> DRAM_CKE,
		sdram_wire_cs_n									=> DRAM_CS_N,
		sdram_wire_dq										=> DRAM_DQ,
		sdram_wire_dqm										=> DRAM_DQM,		
		sdram_wire_ras_n									=> DRAM_RAS_N,
		sdram_wire_we_n									=> DRAM_WE_N,						
		ft232h_nOE                              	=> FT232H_nOE,   
		ft232h_nTXE                             	=> FT232H_nTXE,  
		ft232h_nRXF                             	=> FT232H_nRXF,  
		ft232h_nRD                              	=> FT232H_nRD,
		ft232h_nWR                              	=> FT232H_nWR,  
		ft232h_nSIWU                            	=> FT232H_nSIWU,
		ft232h_ADBUS                            	=> FT232H_ADBUS,
		ft232h_clk_clk                          	=> FT232H_CLK,  				
		led_external_connection_export				=> LED
	);	
	
	u1: component debug_pll port map (
		areset												=> not KEY(0),
		inclk0												=> FT232H_CLK,
		c0														=> DEBUG_CLOCK
	);
				
end rtl;
