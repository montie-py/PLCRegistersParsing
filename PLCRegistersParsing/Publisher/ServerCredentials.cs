namespace PLCRegistersParsing.Publisher;

public sealed record ServerCredentials(
    string Host,
    int Port,
    string Username,
    string Password,
    string UnitNamePrefix,
    int unitsCount = 1
);
