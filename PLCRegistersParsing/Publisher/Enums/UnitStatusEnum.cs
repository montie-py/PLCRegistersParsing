
namespace PLCRegistersParsing.Publisher.Enums
{
    public enum UnitStatusEnum
    {
        WaitingToTransmit,
        WaitingForChallenge,
        Transmitting,
        WaitingForACK,
        Finished,
        ChallengeFailed,
        ACKFailed
    }
}
