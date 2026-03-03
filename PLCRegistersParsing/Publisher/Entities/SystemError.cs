
namespace PLCRegistersParsing.Publisher.Entities
{
    public static class SystemError
    {
        private static readonly string[] ErrorMessages = new[] {
                "CFG_RESET",
                "CFG_CHANGED",
                "CFG_MENU;Operation resumed",
                "CFG_MENU;Operation suspended",
                "POWER_ON;ML-417ADS;V4.4B6",
                "SYS_START",
                "ERR;SERVER_LOGIN;ERROR",
                "ERR;TCP",
                "WDT;SERVER_LOGIN",
                "WDT;Iridium satellite",
                "WDT;TCP_RESPONSE",
                "WDT;APN_LOGIN",
                "WDT;DATA_SENDING",
                "ERR;NTP",
                "ERR;TCP_RESPONSE;NO CARRIER;DCD Dropped",
                "WDT;SIM_DETECT",
                "WDT;NETWORK_REG",
                "WDT;MODEM_INIT",
                "+CME ERROR: SIM failure",
                "ERR;NETWORK_REG;+CGREG: 2,2"
        };

        public static int SystemErrorsQuantity
        {
            get
            {
                return ErrorMessages.Length;
            }
        }

        public static string GetSystemError(int errorNumber)
        {
            string errorMessage = "";

            if (errorNumber < ErrorMessages.Length)
            {
                errorMessage = ErrorMessages[errorNumber];

            }

            return errorMessage;
        }
    }
}
