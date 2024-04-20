set_global_assignment -entity "debug_pll" -library "lib_debug_pll" -name IP_TOOL_NAME "altera_pll"
set_global_assignment -entity "debug_pll" -library "lib_debug_pll" -name IP_TOOL_VERSION "18.1"
set_global_assignment -entity "debug_pll" -library "lib_debug_pll" -name IP_TOOL_ENV "mwpim"
set_global_assignment -library "lib_debug_pll" -name SPD_FILE [file join $::quartus(sip_path) "debug_pll.spd"]

set_global_assignment -library "lib_debug_pll" -name MISC_FILE [file join $::quartus(sip_path) "debug_pll_sim/debug_pll.vho"]
