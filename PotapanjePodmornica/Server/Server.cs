using Server.Helpers;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {

            Console.WriteLine("=== SERVER ZA IGRU 'POTAPANJE PODMORNICA' ==='");

            // unos parametara zadatak 2
            Console.Write("Unesite broj igraca: ");
            int brojIgraca = int.Parse(Console.ReadLine());

            Console.Write("Unesite dimenziju table (npr. 5 za 5x5): ");
            int dimenzija = int.Parse(Console.ReadLine());

            Console.Write("Unesite broj dozvoljenih promasaja: ");
            int promasaji = int.Parse(Console.ReadLine());

            Console.WriteLine($"\nServer postavljen: igraci={brojIgraca}, tabla={dimenzija}x{dimenzija}, dozvoljeni promasaji={promasaji}");

            // kreiranje UDP soketa za prijave
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEP = new IPEndPoint(IPAddress.Any, 15000);
            udpSocket.Bind(udpEP);

            Console.WriteLine($"[UDP] Server ceka prijave igraca na {udpEP}");

            //cuvanje prijava
            List<EndPoint> prijavljeni = new List<EndPoint>();

            while (prijavljeni.Count < brojIgraca)
            {
                byte[] prijemniBafer = new byte[1024];
                EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                int brBajta = udpSocket.ReceiveFrom(prijemniBafer, ref posiljaocEP);

                string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brBajta);
                Console.WriteLine($"[UDP] Primljena prijava: '{poruka}' od {posiljaocEP}");

                if (poruka == "PRIJAVA" && !prijavljeni.Contains(posiljaocEP))
                {
                    prijavljeni.Add(posiljaocEP);
                    Console.WriteLine($"[UDP] Igrac {prijavljeni.Count}/{brojIgraca} prijavljen.");
                }
            }

            Console.WriteLine("\n=== Svi igraci su prijavljeni! ===");

            int tcpPort = 15001;
            string porukaTcp = $"UDP_OK: TCP 127.0.0.1:{tcpPort}";
            byte[] tcpInfo = Encoding.UTF8.GetBytes(porukaTcp);

            foreach (var ep in prijavljeni)
            {
                udpSocket.SendTo(tcpInfo, ep);
            }
            udpSocket.Close();
            Console.WriteLine($"[UDP] Poslati TCP parametri igracima: {porukaTcp}");

            // TCP
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint tcpEP = new IPEndPoint(IPAddress.Any, tcpPort);
            tcpSocket.Bind(tcpEP);
            tcpSocket.Listen(brojIgraca);
            Console.WriteLine($"[TCP] Server slusa na {tcpEP}");

            List<Socket> tcpKlijenti = new List<Socket>();
            while (tcpKlijenti.Count < brojIgraca)
            {
                Socket accepted = tcpSocket.Accept();
                tcpKlijenti.Add(accepted);
                Console.WriteLine($"[TCP] Povezao se igrac {tcpKlijenti.Count}/{brojIgraca} sa {accepted.RemoteEndPoint}");
            }

            // slanje inicijalne poruke
            string initMsg = $"Velicina table je {dimenzija}x{dimenzija}, " + $"posaljite brojevne vrijednosti polja vasih podmornica (1-{dimenzija * dimenzija}). " + $"Ukupno dozvoljen broj promasaja: {promasaji}";
            byte[] initBytes = Encoding.UTF8.GetBytes(initMsg);

            foreach (var klijent in tcpKlijenti)
            {
                klijent.Send(initBytes);
            }
            Console.WriteLine("[TCP] Poslata inicijalna poruka svim igracima!");
        }
    }
}
