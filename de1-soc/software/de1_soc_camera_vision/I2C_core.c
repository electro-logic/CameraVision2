// NB: Taken from Terasic D8M Example

#include "I2C_core.h"
#include <stdio.h>		// alt_printf
#include <unistd.h>		// usleep
#include <io.h>			// IORD, IOWR

// Note. Remember to reset device before access to I2C interface
#ifdef DEBUG_I2C
    #define I2C_DEBUG(x)    DEBUG(x)
#else
    #define I2C_DEBUG(x)
#endif

#define SLEEP_TIME 250  //us
#define ACK_SLEEP_TIME 250 //us
#define TRUE 1
#define FALSE 0

bool Write32_Data(alt_32 base_address, alt_32 offset_address, alt_32 Data){
	bool bPass;
    IOWR(base_address, offset_address,Data);
    bPass=TRUE;
    return bPass;
}

bool Read32_Data(alt_32 base_address, alt_32 offset_address, alt_32 *pData32){
	bool bPass;
	*pData32 =IORD(base_address ,offset_address  );
	bPass=TRUE;
    return bPass;
}

bool oc_i2c_init(alt_32 i2c_base){
	bool bSuccess;
	const alt_32 ref_clk = 50*1000*1000; // 50MHz
	const alt_32 i2c_clk = 400*1000; // 400KHz
	bSuccess = oc_i2c_init_ex(i2c_base, ref_clk, i2c_clk);
	return bSuccess;
}

bool oc_i2c_init_ex(alt_32 i2c_base, alt_32 ref_clk, alt_32 i2c_clk)
{
	bool bSuccess=TRUE;
	alt_32 read_data;
	alt_32 prescale;
	alt_u8 prescale_high;
	alt_u8 prescale_low;
	const alt_u8 ControlValue = 0x80;

    // I2c sysclock =50M hz
	// i2c scl max 400k
	//scl =sysclock/(prescale*5)
	prescale = (ref_clk/(5*i2c_clk))-1;
	prescale_low = prescale & 0xFF;
	prescale_high = (prescale >> 8) & 0xFF;

   IOWR(i2c_base, 0, prescale_low);//write low byte of  prescale (reg 0)
   IOWR(i2c_base, 1, prescale_high);//write high byte of prescale (reg 1)

   //enable the I2C core, but disable the IRQ
   IOWR( i2c_base, 2, ControlValue);

    // check prescale low byte
    if (bSuccess){
    	read_data =IORD(i2c_base, 0);
        if( (read_data & 0x00ff) != prescale_low ){
            bSuccess = FALSE;
        }
    }

    // check prescale high byte
    if (bSuccess){
    	read_data =IORD(i2c_base, 1);
        if( (read_data & 0x00ff) != prescale_high ){
            bSuccess = FALSE;
        }
    }

    // check control
    if (bSuccess){
    	read_data =IORD(i2c_base, 2);
        if( (read_data & 0x00ff) != ControlValue ){
            bSuccess = FALSE;
        }
    }

   if (!bSuccess)
	alt_printf("oc_i2c_init_ex fail\n");

   return bSuccess;
}

bool oc_i2c_uninit(alt_32 i2c_base)
{
	bool bSuccess=TRUE;
	alt_32 read_data;
	const alt_u8 ControlValue = 0x00;

    IOWR( i2c_base, 2, ControlValue);
   	read_data =IORD(i2c_base, 2);
	if( (read_data & 0x00ff) != ControlValue ){
            bSuccess = FALSE;
    }

   if (!bSuccess)
	   alt_printf("oc_i2c_uninit fail\n");

   return bSuccess;
}

bool ACK_single_check(alt_32 i2c_base)
{
	bool bSuccess=TRUE;
	alt_32 read_data;

    usleep(ACK_SLEEP_TIME);
    read_data =IORD(i2c_base, 4);
    if(read_data & 0x02)
            bSuccess = FALSE;
     if (bSuccess){
            usleep(ACK_SLEEP_TIME);
            read_data =IORD(i2c_base, 4);
                if(read_data & 0x80)
                    bSuccess = FALSE;
     }
    return bSuccess;
}

bool ACK_check(alt_32 i2c_base){
    bool bSuccess = FALSE;
    int i=0;
    while(!bSuccess && i++ < 10){
        bSuccess = ACK_single_check(i2c_base);
    }
    return bSuccess;
}

bool OC_I2C_Write(alt_32 i2c_base,alt_u8 device_address,alt_u8 sub_address, alt_u8 *pData, int nWriteLength)
{
    //DWORD reg_data = 0x0;
    int i;

   //set the tx reg audio chip dev address with write bit
   if (!Write32_Data( i2c_base, 3,device_address)){
       alt_printf("OC_I2C_Write err[0]\n");
       return FALSE;
   }
   //set STA and WR bits(bit7 and bit4)
   if (!Write32_Data( i2c_base, 4,0x90)){
       alt_printf("OC_I2C_Write err[1]\n");
       return FALSE;
   }

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2C_Write err[2]\n");
        return FALSE;
    }
  // alt_printf("\n receive ACK-device address! \n");

   //set the txr reg data with reg address + 1 data MSB
   if (!Write32_Data( i2c_base, 3,sub_address)){
       alt_printf("OC_I2C_Write err[3]\n");
       return FALSE;
   }

   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10)){
        alt_printf("OC_I2C_Write err[4]\n");
        return FALSE;
    }

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2C_Write err[5]\n");
        return FALSE;
    }
 //  alt_printf("\n receive ACK-reg address! \n");

#if 1
        for( i=nWriteLength-1;i>=0;i--){
        //set the txr reg data with the data
          if (!Write32_Data( i2c_base, 3,*(pData+i))){
              alt_printf("OC_I2C_Write err[6]\n");
              return FALSE;
          }

        //set STO and WR bits(bit7 and bit4)
         if (!Write32_Data( i2c_base, 4,0x10)){
             alt_printf("OC_I2C_Write err[7]\n");
              return FALSE;
         }

         //wait TIP bit go to 0 to end Tx
          if(!ACK_check( i2c_base)){
              alt_printf("OC_I2C_Write err[8]\n");
              return FALSE;
          }
    }

#else
  //set the txr reg data with the data
    if (!Write32_Data( i2c_base, 3,data&0xff))
        return false;

   //set STO and WR bits(bit7 and bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return false;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return false;
#endif

     if (!Write32_Data( i2c_base, 4,0x40)){
         alt_printf("OC_I2C_Write err[9]\n");
         return FALSE;
     }
     //Sleep(10);
     //OS_msleep(1);
     usleep(SLEEP_TIME);
  // alt_printf("\n receive ACK-data! \n");
     return TRUE;
}

bool ACK_judge_for_read(alt_32 i2c_base)
{
	bool bSuccess;
    alt_32 this_data;
   // OS_msleep( SLEEP_TIME );
     usleep(ACK_SLEEP_TIME);
    //   while(this_data & 0x02)
//   {
//      this_data =  Read32_Data ( hPCIe, i2c_base, 4);
//   }
    bSuccess =  Read32_Data ( i2c_base, 4, &this_data);
    if (bSuccess){
        if(this_data & 0x02)
            bSuccess = FALSE;
    }
   //wait the rx ACK signal 0-valid
    return bSuccess;
}

bool OC_I2C_Read(alt_32 i2c_base,alt_u8 device_address,alt_u8 sub_address, alt_u8 *pData8, int nReadLength){

    //DWORD reg_data = 0x0;
    //BYTE data = 0x0;
    alt_32 Data32;
    int i;

   //set the tx reg audio chip dev address with write bit
   IOWR( i2c_base, 3,device_address);
   //set STA and WR bits(bit7 and bit4)
   IOWR( i2c_base, 4,0x90);
   //wait TIP bit go to 0 to end Tx
   if (!ACK_check( i2c_base)){
       alt_printf("OC_I2C_Read err[2]\n");
       return FALSE;
   }
   IOWR(i2c_base, 3,sub_address);

   //set WR bits(bit4)
   IOWR( i2c_base, 4,0x10);
   //wait TIP bit go to 0 to end Tx
    if (!ACK_check( i2c_base)){
        alt_printf("OC_I2C_Read err[5]\n");
        return FALSE;
    }
  // alt_printf("\n read receive ACK-reg address! \n");

   //read
    //set the tx reg audio chip dev address with read bit 1

    IOWR(i2c_base, 3,device_address|0x01);

   //set STA and WR bits(bit7 and bit4)
    IOWR( i2c_base, 4,0x90);

   //wait TIP bit go to 0 to end Tx
    if (!ACK_check( i2c_base)){
        alt_printf("OC_I2C_Read err[8]\n");
        return FALSE;
    }

    for(i=0;i<nReadLength;i++){
  // alt_printf("\n read receive ACK-device address(read)! \n");
   //set the RD and ACK bit(bit5 and bit3)
    	IOWR( i2c_base, 4,((i+1) == nReadLength)?0x28:0x20);

        if (!ACK_judge_for_read( i2c_base)){
            alt_printf("OC_I2C_Read err[10]\n");
            return FALSE;
        }
   // alt_printf("\n read receive ACK-device address(read)! \n");

        Data32=IORD( i2c_base, 3);
        *(pData8+i) =  Data32 & 0xff;
    }

    IOWR( i2c_base, 4,0x40);

  // Sleep(10);
   //OS_msleep(1);
    usleep(SLEEP_TIME);
 //  alt_printf(" Read  [%02X] = %02Xh\n", sub_address, data);


   return TRUE;

}


bool  OC_I2C_Read_Continue(alt_32 i2c_base,alt_u8 device_address, alt_u8 *pData8, int nReadLength){
	int i;
	alt_u32 Data32;

    IOWR(i2c_base, 3,device_address|0x01);

   //set STA and WR bits(bit7 and bit4)
    IOWR( i2c_base, 4,0x90);
//usleep(5*1000);
   //wait TIP bit go to 0 to end Tx
    if (!ACK_check( i2c_base)){
        alt_printf("OC_I2C_Read err[8]\n");
        return FALSE;
    }

    for(i=0;i<nReadLength;i++){
  // alt_printf("\n read receive ACK-device address(read)! \n");
   //set the RD and ACK bit(bit5 and bit3)
    	IOWR( i2c_base, 4,((i+1) == nReadLength)?0x28:0x20);

        if (!ACK_judge_for_read( i2c_base)){
            alt_printf("OC_I2C_Read err[10]\n");
            return FALSE;
        }
   // alt_printf("\n read receive ACK-device address(read)! \n");

        Data32=IORD( i2c_base, 3);
        *(pData8+i) =  Data32 & 0xff;
    }

    IOWR( i2c_base, 4,0x40);

  // Sleep(10);
   //OS_msleep(1);
    usleep(SLEEP_TIME);
 //  alt_printf(" Read  [%02X] = %02Xh\n", sub_address, data);


   return TRUE;

}


//  size > 2Kb
bool  OC_I2CL_Write(alt_32 i2c_base,alt_u8 device_address,alt_u16 sub_address,alt_u8 *pData, int nWriteLength)
    {
    //DWORD reg_data = 0x0;
	alt_u8 AddrHigh, AddrLow;
    //int count= 0;

    AddrHigh = (sub_address >> 8) & 0xFF;
    AddrLow  = sub_address & 0xFF;

    int i;

    //set the tx reg audio chip dev address with write bit
   if (!Write32_Data( i2c_base, 3,device_address))
       return FALSE;
   //set STA and WR bits(bit7 and bit4)
   if (!Write32_Data( i2c_base, 4,0x90))//0x90
       return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Write err[0]\n");
        return FALSE;
    }
  // alt_printf("\n receive ACK-device address! \n");

   //set the txr reg data with reg address + 1 data MSB
 //  reg_data  = (sub_address << 1) & 0xFE;
   //reg_data |= ((data >> 8) & 0x01);
   if (!Write32_Data( i2c_base, 3,AddrHigh))//reg_data&0xff);
       return FALSE;

   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Write err[1]\n");
        return FALSE;
    }
  // alt_printf("\n receive ACK-reg high address! \n");

   //set the txr reg data with reg address + 1 data MSB
   if (!Write32_Data( i2c_base, 3,AddrLow))//reg_data&0xff);
       return FALSE;

   //set WR bits(bit4)
    Write32_Data( i2c_base, 4,0x10);

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Write err[2]\n");
        return FALSE;
    }
  //  alt_printf("\n receive ACK-reg low address! \n");

#if 1
    for( i=nWriteLength-1;i>=0;i--){
        //set the txr reg data with the other data 8 bit LSB
        if (!Write32_Data( i2c_base, 3,*(pData+i)))
            return FALSE;

       //set STO and WR bits(bit7 and bit4)
        if (!Write32_Data( i2c_base, 4,0x10))
            return FALSE;

        //wait TIP bit go to 0 to end Tx
         if(!ACK_check( i2c_base)){
             alt_printf("OC_I2CL_Write err[3]\n");
             return FALSE;
         }
    }
#else
    //set the txr reg data with the other data 8 bit LSB
    if (!Write32_Data( i2c_base, 3,data&0xff))
        return false;

   //set STO and WR bits(bit7 and bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return FALSE;

#endif
    if (!Write32_Data( i2c_base, 4,0x40))
         return FALSE;

     //OS_msleep(1);
      usleep(SLEEP_TIME);
  //  alt_printf("\n receive ACK-data! \n");

     return TRUE;
}

/*

bool  OC_I2CL_Read(alt_32 i2c_base,alt_u8 device_address,int sub_address, alt_u8 *pData8){

    //DWORD reg_data = 0x0;
    alt_32 Data32;// = 0x0;

    alt_u8 AddrHigh, AddrLow;

    AddrHigh = (sub_address >> 8) & 0xFF;
    AddrLow  = sub_address & 0xFF;


   //set the tx reg audio chip dev address with write bit
   if (!Write32_Data( i2c_base, 3,device_address))
       return FALSE;
   //set STA and WR bits(bit7 and bit4)
   if (!Write32_Data( i2c_base, 4,0x90))
       return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return FALSE;
 //  alt_printf("\n receive ACK-device address! \n");


   //set the txr reg data with reg address + 0

   if (!Write32_Data( i2c_base, 3,AddrHigh))//reg_data&0xff);
       return FALSE;

   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return FALSE;
//   alt_printf("\n read receive ACK-reg High address! \n");

     //set the txr reg data with reg address + 0
   if (!Write32_Data( i2c_base, 3,AddrLow)) //;//reg_data&0xff);
       return FALSE;

   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return FALSE;

 //  alt_printf("\n read receive ACK-reg Low address! \n");

   //read
    //set the tx reg audio chip dev address with read bit 1
     if (!Write32_Data( i2c_base, 3,device_address|0x01))
         return FALSE;

   //set STA and WR bits(bit7 and bit4)
     if (!Write32_Data( i2c_base, 4,0x90))
         return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base))
        return FALSE;
//   alt_printf("\n read receive ACK-device address(read)! \n");

       //read the rxr data


   //set the RD and ACK bit(bit5 and bit3)
    if (!Write32_Data( i2c_base, 4,0x28))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_judge_for_read( i2c_base))
        return FALSE;

//    alt_printf("\n read receive ACK-device address(read)! \n");
    if (!Read32_Data ( i2c_base, 3, &Data32))
        return FALSE;

    *pData8 =  Data32 & 0xff;


   if (!Write32_Data( i2c_base, 4,0x40))
       return FALSE;

   //OS_msleep(1);
    usleep(SLEEP_TIME);

   return TRUE;

} */

bool  OC_I2CL_Read(alt_32 i2c_base,alt_u8 device_address, alt_u16 sub_address, alt_u8 *pData8, int nReadLength){

    //DWORD reg_data = 0x0;
    alt_32 Data32;// = 0x0;
    int i;

    alt_u8 AddrHigh, AddrLow;

    AddrHigh = (sub_address >> 8) & 0xFF;
    AddrLow  = sub_address & 0xFF;


   //set the tx reg audio chip dev address with write bit
   if (!Write32_Data( i2c_base, 3,device_address))
       return FALSE;

   //set STA and WR bits(bit7 and bit4)
   if (!Write32_Data( i2c_base, 4,0x90))
       return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Read err[0]\n");
        return FALSE;
    }
 //  alt_printf("\n receive ACK-device address! \n");


   //set the txr reg data with reg address + 0

   if (!Write32_Data( i2c_base, 3,AddrHigh))//reg_data&0xff);
       return FALSE;

   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Read err[1]\n");
        return FALSE;
    }
//   alt_printf("\n read receive ACK-reg High address! \n");

     //set the txr reg data with reg address + 0
   if (!Write32_Data( i2c_base, 3,AddrLow)) //;//reg_data&0xff);
       return FALSE;
   //set WR bits(bit4)
    if (!Write32_Data( i2c_base, 4,0x10))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Read err[2]\n");
        return FALSE;
    }

 //  alt_printf("\n read receive ACK-reg Low address! \n");

   //read
    //set the tx reg audio chip dev address with read bit 1
     if (!Write32_Data( i2c_base, 3,device_address|0x01))
         return FALSE;

   //set STA and WR bits(bit7 and bit4)
     if (!Write32_Data( i2c_base, 4,0x90))
         return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_check( i2c_base)){
        alt_printf("OC_I2CL_Read err[3]\n");
        return FALSE;
    }
//   alt_printf("\n read receive ACK-device address(read)! \n");

       //read the rxr data

#if 1
    for(i=0;i<nReadLength;i++){
  // alt_printf("\n read receive ACK-device address(read)! \n");
   //set the RD and ACK bit(bit5 and bit3)
    	IOWR( i2c_base, 4,((i+1) == nReadLength)?0x28:0x20);

        if (!ACK_judge_for_read( i2c_base)){
            alt_printf("OC_I2CL_Read err[4]\n");
            return FALSE;
        }
   // alt_printf("\n read receive ACK-device address(read)! \n");

        Data32=IORD( i2c_base, 3);
        *(pData8+i) =  Data32 & 0xff;
    }
#else
   //set the RD and ACK bit(bit5 and bit3)
    if (!Write32_Data( i2c_base, 4,0x28))
        return FALSE;

   //wait TIP bit go to 0 to end Tx
    if(!ACK_judge_for_read( i2c_base))
        return FALSE;

//    alt_printf("\n read receive ACK-device address(read)! \n");
    if (!Read32_Data ( i2c_base, 3, &Data32))
        return FALSE;

    DataHigh =  Data32 & 0xff;

    if (!Read32_Data ( i2c_base, 3, &Data32))
        return FALSE;

    DataLow =  Data32 & 0xff;

    pData16 = (DataHigh << 8) | DataLow;
#endif
   if (!Write32_Data( i2c_base, 4,0x40))
       return FALSE;

   //OS_msleep(1);
   usleep(SLEEP_TIME);
   return TRUE;
}
