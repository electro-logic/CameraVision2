#include <cstdio>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>


#include <stdio.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/mman.h>
//#include "C:/intelFPGA/18.1/embedded/ip/altera/hps/altera_hps/hwlib/include/soc_cv_av/socal/hps.h"
#include <stdint.h>

#define LEDR_BASE	0x10040
#define LEDR_SPAN	0x0F
/*
#define KEY_BASE	0x10010
#define KEY_SPAN	0x0F
#define SW_BASE		0x10040
#define SW_SPAN		0x0F
#define SEG7_BASE	0x10060
#define SEG7_SPAN	0x1F
*/

/* The base address byte offset for the start of the ALT_STM component. */
#define ALT_STM_OFST        0xfc000000

#define ALT_LWFPGASLVS_OFST        0xff200000	// The base address byte offset for the start of the ALT_LWFPGASLVS component
//#define HW_REGS_BASE	ALT_LWFPGASLVS_OFST 	// 0xff200000	Lightweight HPS-to-FPGA AXI address
//#define HW_REGS_SPAN	0x20000					// memory to map, should multiple of 0x1000

#define HW_REGS_BASE ( ALT_STM_OFST )
#define HW_REGS_SPAN ( 0x04000000 )
#define HW_REGS_MASK ( HW_REGS_SPAN - 1 )

int fpga();

int main()
{
	return fpga();
}

int fpga() 
{
	printf("HPC to FPGA control\n");

	uint32_t* h2p_lw_led_addr = NULL;
	void* virtual_base = NULL;
	int fd_dev_mem;

	// Open /dev/mem
	if ((fd_dev_mem = open("/dev/mem", (O_RDWR | O_SYNC))) == -1) {
		printf("ERROR: could not open \"/dev/mem\"...\n");
		return (1);
	}

	// get virtual addr that maps to physical
	virtual_base = mmap(NULL, HW_REGS_SPAN, PROT_READ | PROT_WRITE, MAP_SHARED, fd_dev_mem, HW_REGS_BASE);
	if (virtual_base == MAP_FAILED) {
		printf("ERROR: mmap() failed...\n");
		close(fd_dev_mem);
		return (1);
	}

	// Get the address that maps to the LEDs
	//h2p_lw_led_addr = (uint32_t*)(h2f_lw_axi_master + LEDR_BASE);

	h2p_lw_led_addr = virtual_base + ((unsigned long)(ALT_LWFPGASLVS_OFST + LEDR_BASE) & (unsigned long)(HW_REGS_MASK));

	*(uint32_t*)h2p_lw_led_addr = 0;
	
	/*
	// toggle the LEDs a bit
	int loop_count;
	int led_direction;
	int led_mask;

	loop_count = 0;
	led_mask = 0x01;
	led_direction = 0; // 0: left to right direction
	while (loop_count < 60) {

		// control led
		*(uint32_t*)h2p_lw_led_addr = ~led_mask;

		// wait 100ms
		usleep(100 * 1000);

		// update led mask
		if (led_direction == 0) {
			led_mask <<= 1;
			if (led_mask == (0x01 << (10 - 1)))
				led_direction = 1;
		}
		else {
			led_mask >>= 1;
			if (led_mask == 0x01) {
				led_direction = 0;
				loop_count++;
			}
		}

	} // while
	*/

	if (munmap(virtual_base, LEDR_SPAN) != 0) {
		printf("ERROR: munmap() failed...\n");
		close(fd_dev_mem);
		return (1);
	}

	close(fd_dev_mem);

	printf("End of demo\n");
	return 0;
}

int server()
{
	printf("PC-FPGA ARM over ethernet sample\r\n");
	// Create TCP/IP socket
	int sock = socket(AF_INET, SOCK_STREAM, 0);
	struct sockaddr_in serv_addr;
	serv_addr.sin_family = AF_INET;
	serv_addr.sin_addr.s_addr = htonl(INADDR_ANY);
	serv_addr.sin_port = htons(1111);
	// Bind socket to all IP at port 1111
	bind(sock, (struct sockaddr*)&serv_addr, sizeof(serv_addr));
	// Listen for incoming connections
	listen(sock, 1);
	// Accept incoming connection
	int conn = accept(sock, (struct sockaddr*)NULL, NULL);
	// Read line and print it on console
	char ch;
	while (ch != '\n')
	{
		read(conn, &ch, 1);
		printf("%c", ch);
	}
	// Write "Hello PC" line to client
	char buf[] = "Hello PC\n\r";
	write(conn, buf, strlen(buf));
	// Close connection
	close(conn);
	printf("See http://electro-logic.blogspot.it for updates");
	return 0;
}