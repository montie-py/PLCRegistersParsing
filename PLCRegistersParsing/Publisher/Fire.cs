using PLCRegistersParsing.Publisher.Entities;

namespace PLCRegistersParsing.Publisher;

public class Fire
{
    public Fire()
    {
        var creds = new ServerCredentials( 
            Environment.GetEnvironmentVariable("SERVER_HOST")!, 
            int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT")!),
            Environment.GetEnvironmentVariable("SERVER_USER")!,
            Environment.GetEnvironmentVariable("SERVER_PASS")!,
            Environment.GetEnvironmentVariable("UNIT_NAME_PREFIX")!,
            Environment.GetEnvironmentVariable("UNIT_NAME_SUFFIX") ?? ""
            );

        var publishingOptions = new PublishingOptions(
            Host: creds.Host,
            Port: creds.Port,
            Username: creds.Username,
            Password: creds.Password,
            UnitsCount: creds.UnitsCount,
            UnitNamePrefix: creds.UnitNamePrefix,
            UnitNameSuffix: creds.UnitNameSuffix,
            TransmissionDelay: 
            new Option
            {
                Fixed = true,
                Min = 1,
                Max = 1,
                Name = "TransmissionDelay"
            },
            UnitsQuantity: 
            new Option
            {
                Fixed = true,
                Min = 1,
                Max = 1,
                Name = "Units"
            },
            UnitTransmissionInterval: 
            new Option
            {
                Fixed = true,
                Min = 5,
                Max = 5,
                Name = "UnitTransmissionInterval"
            }
            
        );
    }
}