namespace BQ24195
{
    public enum PacketType
    {
        GetRegisterRequest = 0x01,
        GetRegisterResponse = 0x02,
        GetBatteryAdcRequest = 0x03,
        GetBatteryAdcResponse = 0x04,
        GetUsbHostEnableRequest = 0x05,
        GetUsbHostEnableResponse = 0x06,
        WriteRegisterRequest = 0x07,
        WriteRegisterSuccess = 0x08,
        WriteRegisterFailure = 0x09,
        SetUsbHostEnableRequest = 0xA,
        SetUsbHostEnableAcknowledge = 0xB,
    }
}