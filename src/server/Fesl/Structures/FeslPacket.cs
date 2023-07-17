using System.Text;

namespace Arcadia.Fesl.Structures;

public readonly struct FeslPacket
{
    public FeslPacket(byte[] appData)
    {
        Type = Encoding.ASCII.GetString(appData, 0, 4);

        var firstSplit = Utils.SplitAt(appData, 12);
        Checksum = firstSplit[0][4..];

        var bigEndianChecksum = (BitConverter.IsLittleEndian ? Checksum.Reverse().ToArray() : Checksum).AsSpan();
        Id = BitConverter.ToUInt32(bigEndianChecksum[..4]);
        Length = BitConverter.ToUInt32(bigEndianChecksum[4..]);

        Data = firstSplit[1];
        DataDict = Utils.ParseFeslPacketToDict(Data);
    }

    public FeslPacket(string type, uint id, Dictionary<string, object>? dataDict = null)
    {
        Type = type;
        Id = id;
        DataDict = dataDict ?? new Dictionary<string, object>();
    }

    public async Task<byte[]> ToPacket(uint ticketId)
    {
        var data = Utils.DataDictToPacketString(DataDict).ToString();
        var checksum = PacketUtils.GenerateChecksum(data, Id + ticketId);

        var typeBytes = Encoding.ASCII.GetBytes(Type);
        var dataBytes = Encoding.ASCII.GetBytes(data);

        using var response = new MemoryStream(typeBytes.Length + checksum.Length + dataBytes.Length);

        await response.WriteAsync(typeBytes);
        await response.WriteAsync(checksum);
        await response.WriteAsync(dataBytes);
        await response.FlushAsync();

        return response.ToArray();
    }

    public string Type { get; }
    public uint Id { get; }
    public Dictionary<string, object> DataDict { get; }
    public uint? Length { get; }
    public byte[]? Data { get; }
    public byte[]? Checksum { get; }
}