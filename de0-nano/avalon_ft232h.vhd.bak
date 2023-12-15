-- FT232H Single Channel HiSpeed USB to Multipurpose UART/FIFO IC interface

-- Avalon MM (Memory Mapped) slave Interface:

-- Addr	Register	
-- 0 		Data	  		[D7, D6, D5, D4, D3, D2, D1, D0]
-- 1 		Control 		[WR_EN, RD_EN, TXE, RXF, -, -, -, -]
-- 2		DL0			[]
-- 3		DL1			[]
-- 4		DL2			[]
-- 5		DL3			[]
-- 6		DI0			[]
-- 7		DI1			[]
-- 8		DI2			[]
-- 9		DI3			[]

-- Burst Write (from Memory to PC)
-- Data Lenght	= [DL3,DL2,DL1,DL0]
-- Data Index	= [DI3,DI2,DI1,DI0]

-- Note: always update qsys/sythnesis/submodules/avalon_ft232h.vhd or regenerate QSys

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
use ieee.std_logic_unsigned.all;

entity avalon_ft232h is
	port 
	(			
		avalon_clk 					: in std_logic;								-- 60 MHz from ft232h
	
		-- AVALON SLAVE 			(NIOS is master)
		avalon_reset 				: in std_logic;					
		avalon_address 			: in std_logic_vector(3 downto 0);		
		avalon_write 				: in std_logic;				
		avalon_writedata 			: in std_logic_vector (7 downto 0);				
		avalon_read 				: in std_logic;				
		avalon_readdata 			: out std_logic_vector (7 downto 0);		
		
		-- AVALON MASTER 			(SDRAM is slave)
		avm_address					: out std_logic_vector (31 downto 0);		
		avm_write					: out std_logic;
		avm_writedata				: out std_logic_vector (7 downto 0);
		avm_waitrequest			: in std_logic;
		avm_read						: out std_logic;
		avm_readdata				: in std_logic_vector (7 downto 0);
		avm_readdatavalid			: in std_logic;
		
		nTXE							: in std_logic;								-- Can TX
		nRXF							: in std_logic;								-- Can RX
		nOE							: out std_logic;								-- Output enable
		nRD							: out std_logic;								-- FIFO Buffer Read Enable
		nWR							: out std_logic;								-- FIFO Buffer Write Enable
		nSIWU							: out std_logic;								-- Send Immediate / WakeUp		
		ADBUS							: inout std_logic_vector(7 downto 0)	-- Bidirectional FIFO data		
	);
end entity;

architecture rtl of avalon_ft232h is

	type state_type is (	
		-- Configure component
		idle_state,		
		-- Burst Write from Memory (DMA)
		state_dma_read_req,
		state_dma_read_ack,
		state_dma_read_valid,
		state_dma_tx
	);	
	
	type state_tx_type is (	
		state_tx_busy,
		state_tx_wait,
		state_tx_ready
	);	
	
	signal state			: state_type;
	signal state_tx 		: state_tx_type;
	
	signal data_reg 		: std_logic_vector(7 downto 0);
	signal control_reg 	: std_logic_vector(7 downto 0);	
	signal dl_reg 			: std_logic_vector(31 downto 0);
	signal di_reg 			: std_logic_vector(31 downto 0);
	
	--attribute noprune: boolean; attribute preserve: boolean;		
	--signal debug_avm_address		: std_logic_vector (31 downto 0); attribute noprune of debug_avm_address: signal is true; attribute preserve of debug_avm_address: signal is true; 		
	--signal debug_avm_readdata		: std_logic_vector (7 downto 0); attribute noprune of debug_avm_readdata: signal is true; attribute preserve of debug_avm_readdata: signal is true; 		
	--signal debug_avm_read			: std_logic; attribute noprune of debug_avm_read: signal is true; attribute preserve of debug_avm_read: signal is true; 			
	--signal debug_avm_waitrequest	: std_logic; attribute noprune of debug_avm_waitrequest: signal is true; attribute preserve of debug_avm_waitrequest: signal is true; 				
	--signal debug_avm_readdatavalid	: std_logic; attribute noprune of debug_avm_readdatavalid: signal is true; attribute preserve of debug_avm_readdatavalid: signal is true; 									
			
begin
	
	nSIWU <= '1';	

--	-- Debug registers process
--	process(all)
--	begin
--		--if (rising_edge(avalon_clk)) then		
--			debug_avm_address 		<= avm_address;
--			debug_avm_readdata 		<= avm_readdata;
--			debug_avm_read				<= avm_read;
--			debug_avm_waitrequest	<=	avm_waitrequest;
--			debug_avm_readdatavalid	<= avm_readdatavalid;
--		--end if;	
--	end process;
	
	-- Read registers process (available on every state)
	process(all)
	begin	
		if (rising_edge(avalon_clk) and avalon_read = '1') then
			-- Read registers			
			case avalon_address is
				when "0000" => avalon_readdata <= data_reg;
				when "0001" => avalon_readdata <= control_reg;					
				when "0010" => avalon_readdata <= dl_reg(7 downto 0);
				when "0011" => avalon_readdata <= dl_reg(15 downto 8);
				when "0100" => avalon_readdata <= dl_reg(23 downto 16);					
				when "0101" => avalon_readdata <= dl_reg(31 downto 24);					
				when "0110" => avalon_readdata <= di_reg(7 downto 0);
				when "0111" => avalon_readdata <= di_reg(15 downto 8);
				when "1000" => avalon_readdata <= di_reg(23 downto 16);					
				when "1001" => avalon_readdata <= di_reg(31 downto 24);
				when others => null;
			end case;
		end if;
	end process;	
	
	-- Tx state process
	process (all)
	begin	
		if avalon_reset = '1' then
			state_tx		<= state_tx_busy;
		elsif (rising_edge(avalon_clk)) then
			case state_tx is
				when state_tx_busy =>
					if nTXE = '0' then
						state_tx <= state_tx_wait;
					else
						state_tx <= state_tx_busy;
					end if;
				when state_tx_wait =>
					if nTXE = '0' then
						state_tx <= state_tx_ready;
					else
						state_tx <= state_tx_busy;
					end if;
				when state_tx_ready =>			
					if nTXE = '1' then						
						state_tx <= state_tx_busy;
					end if;
			end case;
		end if;
	end process;
	
	-- Sequential Logic
	process (avalon_clk, avalon_reset)
   begin			
      if avalon_reset = '1' then         
			data_reg 	<= (others=>'0');
			control_reg <= (others=>'0');
			dl_reg		<= (others=>'0');
			di_reg		<= (others=>'0');			
			state 		<= idle_state;
      elsif (rising_edge(avalon_clk)) then			
		
			-- Update Control Register
			control_reg(5) <= nTXE;
			control_reg(4) <= nRXF;				
			
			case state is
				when idle_state =>																					
					-- Write registers
					if avalon_write = '1' then					
						case to_integer(unsigned(avalon_address)) is							
							when 0 => data_reg <= avalon_writedata;
							when 1 => 
								control_reg <= avalon_writedata;
								-- Write Enable
								if ((avalon_writedata(7) = '1') and (unsigned(di_reg) < unsigned(dl_reg))) then
									state <= state_dma_read_req;
								end if;
							when 2 => dl_reg(7 downto 0)		<= avalon_writedata;
							when 3 => dl_reg(15 downto 8) 	<= avalon_writedata;
							when 4 => dl_reg(23 downto 16)	<= avalon_writedata;
							when 5 => dl_reg(31 downto 24)	<= avalon_writedata;							
							when 6 => di_reg(7 downto 0)		<= avalon_writedata;
							when 7 => di_reg(15 downto 8)		<= avalon_writedata;
							when 8 => di_reg(23 downto 16)	<= avalon_writedata;
							when 9 => di_reg(31 downto 24)	<= avalon_writedata;
							when others => null;								
						end case;				
					end if;
															
				when state_dma_read_req =>
					if avm_waitrequest = '0' then
						state <= state_dma_read_ack;		
					end if;					
					
				when state_dma_read_ack =>
					if avm_readdatavalid = '1' then
						state 	<= state_dma_read_valid;
						data_reg	<= avm_readdata;
					end if;
					
				when state_dma_read_valid =>												
					if state_tx = state_tx_ready then
						di_reg	<= std_logic_vector(unsigned(di_reg) + 1);					
						state 	<= state_dma_tx;
					end if;
				
				when state_dma_tx =>
					if state_tx = state_tx_ready then
						if(unsigned(di_reg) < unsigned(dl_reg)) then
							state <= state_dma_read_req;							
						else						
							control_reg(7)	<= '0';		-- Write Enable clear
							state 			<= idle_state;
						end if;						
					end if;
					
			end case;			
			
		end if;   					
	end process;
		
	-- Combinatorial logic
	process (all)
	begin					
		nOE <= '1';
		nRD <= '1';
		nWR <= '1';
		ADBUS <= (others => 'Z');					
		avm_write 		<= '0';
		avm_read			<= '0';	
		avm_writedata	<= (others => 'Z');
		avm_address		<= di_reg;
		case state is						
			when state_dma_read_req =>
				avm_read		<= '1';
			when state_dma_read_ack => 	
				null;		
			when state_dma_read_valid =>
				null;						
			when state_dma_tx =>
				nWR 			<= '0';
				ADBUS 		<= data_reg;											
			when others => 
				null;			
		end case;

		-- Monitor nRXF and nTXE
		if(nRXF = '1') then
			nOE <= '1';
			nRD <= '1';
		end if;		
		
		if(nTXE = '1') then
			nWR <= '1';			
			ADBUS <= (others => 'Z');
		end if;
		
	end process;
	
end rtl;