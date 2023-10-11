@ REM Ignore <CR> in DOS
@ set SHELLOPTS=igncr

@ REM Ignore mixed paths (ex. G:/$SOPC_KIT_NIOS2/bin)
@ set CYGWIN=nodosfilewarning

@set QUARTUS_BIN=%QUARTUS_ROOTDIR%\\bin
@if exist %QUARTUS_BIN%\\quartus_pgm.exe (goto program)

@set QUARTUS_BIN=%QUARTUS_ROOTDIR%\\bin64
@if exist %QUARTUS_BIN%\\quartus_pgm.exe (goto program)

:: Prepare for future use (if exes are in bin32)
@set QUARTUS_BIN=%QUARTUS_ROOTDIR%\\bin32

:program
%QUARTUS_BIN%\\quartus_pgm.exe -m jtag -c 1 -o "p;DE0_NANO_D8M.sof@1"