using System.Collections.Generic;

namespace CyberLab.Resources.Libraries
{
    /// <summary>
    /// Klasa zawierająca stałe z komendami SCPI dla Analizatora Widma Optycznego Anritsu MS9740A.
    /// Zaktualizowano na podstawie dokumentu "MS9740A Optical Spectrum Analyzer Remote Control Operation Manual" (M-W3329AE-13.0).
    /// </summary>
    public static class AnritsuMs9740AScpiCommands
    {
        public const string CLEAR_STATUS = "*CLS";
        public const string EVENT_STATUS_ENABLE = "*ESE";
        public const string EVENT_STATUS_ENABLE_QUERY = "*ESE?";
        public const string STANDARD_EVENT_STATUS_REGISTER_QUERY = "*ESR?";
        public const string IDENTIFICATION_QUERY = "*IDN?";
        public const string OPERATION_COMPLETE = "*OPC";
        public const string OPERATION_COMPLETE_QUERY = "*OPC?";
        public const string OPTION_IDENTIFICATION_QUERY = "*OPT?";
        public const string RESET = "*RST";
        public const string SERVICE_REQUEST_ENABLE = "*SRE";
        public const string SERVICE_REQUEST_ENABLE_QUERY = "*SRE?";
        public const string STATUS_BYTE_QUERY = "*STB?";
        public const string SELF_TEST_QUERY = "*TST?";
        public const string WAIT_TO_CONTINUE = "*WAI";

        public const string SPECTRUM_ANALYSIS = "ANA";
        public const string SPECTRUM_ANALYSIS_QUERY = "ANA?";
        public const string SPECTRUM_ANALYSIS_RESULT_QUERY = "ANAR?";

        public const string APPLICATION_SWITCH_AP = "AP";
        public const string APPLICATION_SWITCH_QUERY_AP = "AP?";
        public const string APPLICATION_RESULT_QUERY = "APR?";

        public const string DELTA_MARKER = "DMK";
        public const string DELTA_MARKER_QUERY = "DMK?";
        public const string ERASE_MARKER = "EMK";
        public const string FORMULA = "FML";
        public const string FORMULA_QUERY = "FML?";
        public const string WAVELENGTH_MARKER_A = "MKA";
        public const string WAVELENGTH_MARKER_A_QUERY = "MKA?";
        public const string WAVELENGTH_MARKER_B = "MKB";
        public const string WAVELENGTH_MARKER_B_QUERY = "MKB?";
        public const string LEVEL_MARKER_C = "MKC";
        public const string LEVEL_MARKER_C_QUERY = "MKC?";
        public const string LEVEL_MARKER_D = "MKD";
        public const string LEVEL_MARKER_D_QUERY = "MKD?";
        public const string MARKER_VALUE_WL_FREQ_SELECT = "MKV";
        public const string MARKER_VALUE_WL_FREQ_SELECT_QUERY = "MKV?";
        public const string PEAK_TO_CENTER = "PKC";
        public const string PEAK_TO_LEVEL = "PKL";
        public const string PEAK_SEARCH = "PKS";
        public const string PEAK_SEARCH_QUERY = "PKS?";
        public const string DIP_SEARCH = "DPS";
        public const string DIP_SEARCH_QUERY = "DPS?";
        public const string PEAK_TO_PEAK_CALCULATION = "PPC";
        public const string PEAK_TO_PEAK_CALCULATION_QUERY = "PPC?";
        public const string PEAK_TO_PEAK_MAKER_QUERY = "PPMK?";
        public const string TRACE_MARKER = "TMK";
        public const string TRACE_MARKER_QUERY = "TMK?";
        public const string ZONE_MARKER = "ZMK";
        public const string ZONE_MARKER_QUERY = "ZMK?";

        public const string ALIGN_WITH_CAL = "ACAL";
        public const string ALIGN_WITH_CAL_QUERY = "ACAL?";
        public const string AUTO_ALIGNMENT = "ALIN";
        public const string AUTO_ALIGNMENT_QUERY = "ALIN?";
        public const string AUTO_OFFSET = "AOFS";
        public const string AUTO_OFFSET_QUERY = "AOFS?";
        public const string RESOLUTION_CALIBRATION = "RCAL";
        public const string RESOLUTION_CALIBRATION_QUERY = "RCAL?";
        public const string WAVELENGTH_CALIBRATION = "WCAL";
        public const string WAVELENGTH_CALIBRATION_QUERY = "WCAL?";
        public const string ZERO_CALIBRATION = "ZCAL";
        public const string ZERO_CALIBRATION_QUERY = "ZCAL?";

        public const string DISPLAY_MODE = "DSP";
        public const string DISPLAY_MODE_QUERY = "DSP?";
        public const string ERASE_OVERLAP = "EOV";
        public const string LEVEL_SCALE_QUERY = "LVS?";
        public const string LINEAR_SCALE = "LLV";
        public const string LINEAR_SCALE_QUERY = "LLV?";
        public const string LOG_SCALE = "LOG";
        public const string LOG_SCALE_QUERY = "LOG?";
        public const string REFERENCE_LEVEL = "RLV";
        public const string REFERENCE_LEVEL_QUERY = "RLV?";
        public const string TRACE_DISPLAY = "TMD";
        public const string TRACE_DISPLAY_QUERY = "TMD?";

        public const string COPY_IMAGE_DATA = "CPCOPYDAT";
        public const string COPY_CSV_DATA = "CPCSV";
        public const string COPY_SYSTEM_INFO = "CPSYSINFO";
        public const string COPY_XML_DATA = "CPXML";
        public const string DELETE_IMAGE_DATA = "DELCOPYDAT";
        public const string DELETE_CSV_DATA = "DELCSV";
        public const string DELETE_SYSTEM_INFO = "DELSYSINFO";
        public const string DELETE_XML_DATA = "DELXML";
        public const string LIST_IMAGE_DATA_QUERY = "LISTCOPYDAT?";
        public const string LIST_CSV_DATA_QUERY = "LISTCSV?";
        public const string LIST_SYSTEM_INFO_QUERY = "LISTSYSINFO?";
        public const string LIST_XML_DATA_QUERY = "LISTXML?";
        public const string MOVE_IMAGE_DATA = "MVCOPYDAT";
        public const string MOVE_CSV_DATA = "MVCSV";
        public const string MOVE_SYSTEM_INFO = "MVSYSINFO";
        public const string MOVE_XML_DATA = "MVXML";
        public const string PROTECT_IMAGE_DATA = "PRTCOPYDAT";
        public const string PROTECT_IMAGE_DATA_QUERY = "PRTCOPYDAT?";
        public const string PROTECT_CSV_DATA = "PRTCSV";
        public const string PROTECT_CSV_DATA_QUERY = "PRTCSV?";
        public const string PROTECT_SYSTEM_INFO = "PRTSYSINFO";
        public const string PROTECT_SYSTEM_INFO_QUERY = "PRTSYSINFO?";
        public const string PROTECT_XML_DATA = "PRTXML";
        public const string PROTECT_XML_DATA_QUERY = "PRTXML?";
        public const string SAVE_CSV_DATA = "SVCSV";
        public const string SAVE_CSV_ALL_DATA = "SVCSVA";
        public const string SAVE_XML_DATA = "SVXML";
        public const string RECALL_XML_DATA = "RCXML";
        public const string GET_BINARY_IMAGE_DATA_QUERY = "GHC?";

        public const string AUTO_MEASURE = "AUT";
        public const string AUTO_MEASURE_QUERY = "AUT?";
        public const string OPTICAL_ATTENUATOR = "ATT";
        public const string OPTICAL_ATTENUATOR_QUERY = "ATT?";
        public const string SWEEP_AVERAGE = "AVS";
        public const string SWEEP_AVERAGE_QUERY = "AVS?";
        public const string POINT_AVERAGE = "AVT";
        public const string POINT_AVERAGE_QUERY = "AVT?";
        public const string CENTER_WAVELENGTH = "CNT";
        public const string CENTER_WAVELENGTH_QUERY = "CNT?";
        public const string DYNAMIC_RANGE_MODE = "DRG";
        public const string DYNAMIC_RANGE_MODE_QUERY = "DRG?";
        public const string INTERVAL_TIME = "ITM";
        public const string INTERVAL_TIME_QUERY = "ITM?";
        public const string LEVEL_OFFSET = "LOFS";
        public const string LEVEL_OFFSET_QUERY = "LOFS?";
        public const string MODULATION_MODE = "MDM";
        public const string MODULATION_MODE_QUERY = "MDM?";
        public const string MULTIMODE_FIBER_MODE = "MMM";
        public const string MULTIMODE_FIBER_MODE_QUERY = "MMM?";
        public const string MEASURE_MODE_QUERY = "MOD?";
        public const string SAMPLING_POINTS = "MPT";
        public const string SAMPLING_POINTS_QUERY = "MPT?";
        public const string RESOLUTION = "RES";
        public const string RESOLUTION_QUERY = "RES?";
        public const string ACTUAL_RESOLUTION_DISPLAY = "ARES";
        public const string ACTUAL_RESOLUTION_DISPLAY_QUERY = "ARES?";
        public const string ACTUAL_RESOLUTION_DATA_QUERY = "ARED?";
        public const string SMOOTHING = "SMT";
        public const string SMOOTHING_QUERY = "SMT?";
        public const string SPAN_WAVELENGTH = "SPN";
        public const string SPAN_WAVELENGTH_QUERY = "SPN?";
        public const string START_WAVELENGTH = "STA";
        public const string START_WAVELENGTH_QUERY = "STA?";
        public const string STOP_WAVELENGTH = "STO";
        public const string STOP_WAVELENGTH_QUERY = "STO?";
        public const string REPEAT_SWEEP = "SRT";
        public const string SINGLE_SWEEP = "SSI";
        public const string SWEEP_STOP = "SST";
        public const string SEARCH_THRESHOLD = "STHR";
        public const string SEARCH_THRESHOLD_QUERY = "STHR?";
        public const string SEARCH_THRESHOLD_SET = "STHRS";
        public const string SEARCH_THRESHOLD_SET_QUERY = "STHRS?";
        public const string VIDEO_BANDWIDTH = "VBW";
        public const string VIDEO_BANDWIDTH_QUERY = "VBW?";
        public const string WAVELENGTH_DISPLAY_AIR_VAC = "WDP";
        public const string WAVELENGTH_DISPLAY_AIR_VAC_QUERY = "WDP?";
        public const string WAVELENGTH_OFFSET = "WOFS";
        public const string WAVELENGTH_OFFSET_QUERY = "WOFS?";
        public const string WAVELENGTH_START_STOP = "WSS";
        public const string WAVELENGTH_START_STOP_QUERY = "WSS?";

        public const string EXTENDED_EVENT_STATUS_ENABLE_2 = "ESE2";
        public const string EXTENDED_EVENT_STATUS_ENABLE_2_QUERY = "ESE2?";
        public const string EXTENDED_EVENT_STATUS_ENABLE_3 = "ESE3";
        public const string EXTENDED_EVENT_STATUS_ENABLE_3_QUERY = "ESE3?";
        public const string EXTENDED_EVENT_STATUS_REGISTER_2_QUERY = "ESR2?";
        public const string EXTENDED_EVENT_STATUS_REGISTER_3_QUERY = "ESR3?";
        public const string ERROR_QUERY = "ERR?";

        public const string BUZZER = "BUZ";
        public const string BUZZER_QUERY = "BUZ?";
        public const string IMAGE_COLOR_SETTING = "COLOR";
        public const string IMAGE_COLOR_SETTING_QUERY = "COLOR?";
        public const string DELIMITER = "DELM";
        public const string DELIMITER_QUERY = "DELM?";
        public const string FORMAT_OF_IMAGE_FILE = "PMOD";
        public const string FORMAT_OF_IMAGE_FILE_QUERY = "PMOD?";
        public const string PRESET = "PRE";
        public const string SAVE_IMAGE_DATA = "PRINT";
        public const string SOFTWARE_VERSION_QUERY = "SOFTVER?";
        public const string APPLICATION_SWITCH_SYS = "SYS";
        public const string APPLICATION_SWITCH_QUERY_SYS = "SYS?";
        public const string SYSTEM_INFORMATION_QUERY = "SYSINFO?";
        public const string TERMINATOR = "TRM";
        public const string TERMINATOR_QUERY = "TRM?";

        public const string MEMORY_DATA_A_BINARY_QUERY = "DBA?";
        public const string MEMORY_DATA_B_BINARY_QUERY = "DBB?";
        public const string MEMORY_DATA_C_BINARY_QUERY = "DBC?";
        public const string MEMORY_DATA_D_BINARY_QUERY = "DBD?";
        public const string MEMORY_DATA_E_BINARY_QUERY = "DBE?";
        public const string MEMORY_DATA_F_BINARY_QUERY = "DBF?";
        public const string MEMORY_DATA_G_BINARY_QUERY = "DBG?";
        public const string MEMORY_DATA_H_BINARY_QUERY = "DBH?";
        public const string MEMORY_DATA_I_BINARY_QUERY = "DBI?";
        public const string MEMORY_DATA_J_BINARY_QUERY = "DBJ?";
        public const string DATA_CONDITION_A_QUERY = "DCA?";
        public const string DATA_CONDITION_B_QUERY = "DCB?";
        public const string DATA_CONDITION_C_QUERY = "DCC?";
        public const string DATA_CONDITION_D_QUERY = "DCD?";
        public const string DATA_CONDITION_E_QUERY = "DCE?";
        public const string DATA_CONDITION_F_QUERY = "DCF?";
        public const string DATA_CONDITION_G_QUERY = "DCG?";
        public const string DATA_CONDITION_H_QUERY = "DCH?";
        public const string DATA_CONDITION_I_QUERY = "DCI?";
        public const string DATA_CONDITION_J_QUERY = "DCJ?";
        public const string MEMORY_DATA_A_TEXT_QUERY = "DMA?";
        public const string MEMORY_DATA_B_TEXT_QUERY = "DMB?";
        public const string MEMORY_DATA_C_TEXT_QUERY = "DMC?";
        public const string MEMORY_DATA_D_TEXT_QUERY = "DMD?";
        public const string MEMORY_DATA_E_TEXT_QUERY = "DME?";
        public const string MEMORY_DATA_F_TEXT_QUERY = "DMF?";
        public const string MEMORY_DATA_G_TEXT_QUERY = "DMG?";
        public const string MEMORY_DATA_H_TEXT_QUERY = "DMH?";
        public const string MEMORY_DATA_I_TEXT_QUERY = "DMI?";
        public const string MEMORY_DATA_J_TEXT_QUERY = "DMJ?";
        public const string MEMORY_DATA_A_CSV_QUERY = "DQA?";
        public const string MEMORY_DATA_B_CSV_QUERY = "DQB?";
        public const string MEMORY_DATA_C_CSV_QUERY = "DQC?";
        public const string MEMORY_DATA_D_CSV_QUERY = "DQD?";
        public const string MEMORY_DATA_E_CSV_QUERY = "DQE?";
        public const string MEMORY_DATA_F_CSV_QUERY = "DQF?";
        public const string MEMORY_DATA_G_CSV_QUERY = "DQG?";
        public const string MEMORY_DATA_H_CSV_QUERY = "DQH?";
        public const string MEMORY_DATA_I_CSV_QUERY = "DQI?";
        public const string MEMORY_DATA_J_CSV_QUERY = "DQJ?";
        public const string STORAGE_MODE = "SMD";
        public const string STORAGE_MODE_QUERY = "SMD?";
        public const string TRACE_SELECT = "TSL";
        public const string TRACE_SELECT_QUERY = "TSL?";
        public const string TITLE_ERASE = "TER";
        public const string TITLE = "TTL";
        public const string TITLE_QUERY = "TTL?";
        public const string TRACE_TYPE = "TTP";
        public const string TRACE_TYPE_QUERY = "TTP?";

        public const string EXT_TRIGGER_DELAY_TIME = "TDL";
        public const string EXT_TRIGGER_DELAY_TIME_QUERY = "TDL?";

        public const string POWER_MONITOR = "PWR";
        public const string POWER_MONITOR_QUERY = "PWR?";
        public const string POWER_MONITOR_RESULT_QUERY = "PWRR?";
        public const string SPECTRUM_MODE = "SPC";

        public const string LIGHT_OUTPUT = "OPT";
        public const string LIGHT_OUTPUT_QUERY = "OPT?";
    }

    public static class AnritsuMs9740ATimeConsts
    {
        public static readonly Dictionary<int, double> VBWcoeff = new Dictionary<int, double>
        {
            { 10, 32.0 },
            { 100, 3.5 },
            { 200, 2.0 },
            { 1000, 0.5 },
            { 2000, 0.3 },
            { 10000, 0.2 },
            { 100000, 0.2 },
            { 1000000, 0.2 }
        };

        public static readonly Dictionary<int, double> SamplingPointsCoeff = new Dictionary<int, double>
        {
            { 51, 0.1 },
            { 101, 0.2 },
            { 251, 0.5 },
            { 501, 1 },
            { 1001, 2 },
            { 2001, 4 },
            { 5001, 10 },
            { 10001, 20 },
            { 20001, 40 },
            { 50001, 100 }
        };
    }
}

