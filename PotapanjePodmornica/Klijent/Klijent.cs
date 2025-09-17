namespace Klijent
{
    public class Klijent
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== KLIJENT ZA IGRU 'POTAPANJE PODMORNICA' ===");

            // kreiranje UDP socketa za prijavu
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // IP adresa servera
            IPEndPoint serverUdp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 15000);

            Console.WriteLine("Pritisnite Enter da posaljete prijavu serveru...");
            Console.ReadKey();

            string prijava = "PRIJAVA";
            byte[] prijavaBytes = Encoding.UTF8.GetBytes(prijava);

            udpSocket.SendTo(prijavaBytes, serverUdp);
            Console.WriteLine($"Prijava poslata serveru na {serverUdp}");

            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] prijemniBafer = new byte[1024];
            int brBajta = udpSocket.ReceiveFrom(prijemniBafer, ref remoteEP);

            string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
            Console.WriteLine($"[UDP] Odgovor servera: {odgovor}");
            udpSocket.Close();
        }
    }
}
