using System.Net.Sockets;

namespace PLCRegistersParsing.Publisher.Services
{
    public static class TcpService
    {
        public static async Task Connect(TcpClient client, string server, int port, CancellationToken ct)
        {
            var connectTask = client.ConnectAsync(server, port, ct);
            try
            {
                await connectTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Connection to devio cancelled: timeout");
                throw;
            }
        }

        public static async Task SendData(TcpClient client, byte[] content, CancellationToken ct)
        {
           await client.Client.SendAsync(content, SocketFlags.None, ct);
        }

        public static async Task<byte[]> ReadData(TcpClient client, int timeout)
        {
            // Waits for the data reading
            byte[] data = await ReceiveData(client, timeout);

            return data;
        }

        private static async Task<byte[]> ReceiveData(TcpClient client, int timeout)
        {
            List<byte> bufferList = new List<byte>();

            NetworkStream stream = client.GetStream();
            int bufferSize = 1;

            if (stream.DataAvailable)
            {
                bufferSize = client.Available;
            }

            byte[] buffer = new byte[bufferSize];

            stream.ReadTimeout = timeout;

            do
            {
                await stream.ReadExactlyAsync(buffer, 0, bufferSize);
                bufferList.AddRange(buffer);
                bufferSize = client.Available;
                buffer = new byte[bufferSize];

            } while (stream.DataAvailable);

            if (bufferList.Count > 0)
            {

                return bufferList.ToArray();
            }

            throw new Exception();
        }

        public static void CloseConnection(TcpClient client)
        {
            client.Close();
        }

    }
}
