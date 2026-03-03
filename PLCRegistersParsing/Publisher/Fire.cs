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
            Environment.GetEnvironmentVariable("UNIT_NAME_PREFIX")!
            );
    }
}