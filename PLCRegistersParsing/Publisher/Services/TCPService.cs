using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PLCRegistersParsing.Publisher.Services
{
    public static class TCPService
    {
        public static void Connect(TcpClient client, string server, int port)
        {
            client.Connect(server, port);
        }

        public static void SendData(TcpClient client, byte[] content)
        {
            client.Client.Send(content, content.Length, SocketFlags.None);
        }

        public static async Task<byte[]> ReadData(TcpClient client, int timeout)
        {
            // Waits for the data reading
            byte[] data = await Task.Factory.StartNew(() => ReceiveData(client, timeout)).Result;

            return data;
        }

        private static async Task<byte[]> ReceiveData(TcpClient client, int timeout)
        {
            List<byte> bufferList = new List<byte>();

            NetworkStream stream = client.GetStream();
            int bufferSize = 1;

            if (stream.DataAvailable == true)
            {
                bufferSize = client.Available;
            }

            byte[] buffer = new byte[bufferSize];

            stream.ReadTimeout = timeout;

            do
            {
                stream.Read(buffer, 0, bufferSize);
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
