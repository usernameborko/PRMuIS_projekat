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
        }
    }
}
