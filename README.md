#Camera Vision 2

Connect a modern, high-resolution camera to an FPGA and easily develop your applications

**Please read http://electro-logic.blogspot.it for more documentation, articles, etc..**

![alt text](docs/de0-nano_d8m_um232h.jpg)

![alt text](docs/camera_vision2_gui.png)

This project is the evolution of Camera Vision 1 bringing more details (10 bit versus 8 bit) and a faster communication (USB 2.0 versus USB 1.0) 

What's new:

- Full 10 bit support
- JTAG / JTAG + Fast USB 2.0 communication options
- TIFF + RAW export with metadata
- Image Statistics, Download progress
- New MIPI and Camera registers
- Updated toolchain for a modern development environment

Required Software:

- Microsoft Visual Studio 2022 (.NET 6)
- Intel Quartus Prime 18.1

Required Hardware:

- Terasic DE0-Nano
- Terasic D8M (OV8865 Image Sensor + MIPI Decoder TC358748XBG)

Optional Hardware:
- FTDI UM232H with DE0-Nano board adapter (https://electro-logic.blogspot.com/2014/03/fpga-comunicazione-ad-alta-velocita_99.html)

Quick start:

1) Connect D8M into GPIO0 of DE0-Nano like shown into images into \doc folder 
2) Connect DE0-Nano to PC with the USB cable bundled

Optional
2b) Connect UM232H with the adapter into GPIO1
2c) Connect UM232H to PC with an USB 2.0 cable bundled

3) Load with Quartus Programmer \eda\de0-nano\output_files\DE0_NANO_D8M.sof into DE0-Nano
4) Wait that LED0 turn on and launch \sw\CameraVision\CameraVision\bin\Release\CameraVision.exe
5) Press Update into Image panel to take a new image

F.A.Q.

Q) When I launch CameraVision.exe image is corrupted.
A) Likely the MIPI Integrated Circuit on the D8M is out-of-sync. Reprogram the FPGA or Try to press KEY0 on DE0-NANO to reset the system and launch again the software.

Q) How can I edit the Nios II firmware?
A) Open Quartus, click on Tools / Nios II Software Build Tools for Eclipse, File / Import, General / Existing Projects into Workspace, Select root directory: D:\FPGA\CameraVision2\eda\de0-nano\software where the path is you project location

Q) What's next?
A) There is room for some improvements:
- FT232H controller: fully utilise the USB 2.0 bandwidth by adding a FIFO memory to decouple the SDRAM memory
- Improve the protocol to use a single cable (ex. JTAG only or FT232H only)
- Add parameters to customize the mipi controller and the ft232h component from the Platform Designer
- Additional image statistics
- Video features