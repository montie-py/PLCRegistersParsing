using PLCRegistersParsing.Publisher.Entities;

namespace PLCRegistersParsing.Publisher;

public sealed record PublishingOptions(
    string Host,
    int Port,
    string Username,
    string Password,
    string UnitNamePrefix,
    string UnitNameSuffix,
    int UnitsCount,
    bool UseEncryption = true,
    Option TransmissionDelay = null!,
    Option UnitsQuantity = null!,
    Option UnitTransmissionInterval = null!,
    Option NumberParameters = null!,
    Option MeasurementsInterval = null!,
    Option WaitChallenge = null!,
    Option WaitAck = null!
    );