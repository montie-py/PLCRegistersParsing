using PLCRegistersParsing.Publisher.Entities;

namespace PLCRegistersParsing.Publisher;

public sealed record Options(
    string Host,
    int Port,
    string Username,
    string Password,
    string UnitNamePrefix,
    string UnitNameSuffix,
    int UnitsCount,
    int TransmissionDelay,
    int UnitsQuantity,
    int UnitTransmissionInterval,
    int MeasurementsTimeInterval,
    int WaitChallenge,
    int WaitAck,
    bool UseEncryption = true
    );