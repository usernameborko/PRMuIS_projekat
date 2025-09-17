using Klijent.Helpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Klijent
{
    enum StanjeKlijenta
    {
        Idle,
        BiranjeMeta,
        UnosPolja
    }
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

            string[] dijelovi = odgovor.Split(' ');
            string tcpInfo = dijelovi[2];
            string[] ipPort = tcpInfo.Split(':');
            string ip = ipPort[0];
            int port = int.Parse(ipPort[1]);

            // TCP konekcija
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            Console.WriteLine($"[TCP] Povezan na server {ip}:{port}");

            // primanje inicijalne poruke sa servera
            byte[] recvBuffer = new byte[1024];
            int primljeno = clientSocket.Receive(recvBuffer);
            string initMsg = Encoding.UTF8.GetString(recvBuffer, 0, primljeno);
            Console.WriteLine($"[TCP] Server: {initMsg}");

            // parsirnje inicijalne poruke
            int dimenzija = 0;
            int promasaji = 0;

            string[] parts = initMsg.Split(",");
            foreach (var part in parts)
            {
                if (part.Contains("Velicina table"))
                {
                    string vel = part.Split(' ')[3];
                    string[] nums = vel.Split('x');
                    dimenzija = int.Parse(nums[0]);
                }

                if (part.Contains("promasaja"))
                {
                    string broj = part.Split(':')[1].Trim();
                    promasaji = int.Parse(broj);
                }
            }

            // unos podmornica
            int brojPodmornica = Math.Max(1, dimenzija / 2);

            List<List<int>> svePodmornice = new List<List<int>>();

            for (int i = 0; i < brojPodmornica; i++)
            {
                while (true)
                {
                    try
                    {
                        Console.Write($"\nUnesite polje za podmornicu {i + 1}/{brojPodmornica}: ");
                        int start = int.Parse(Console.ReadLine());

                        Console.WriteLine("Unesite orijentaciju: ");
                        Console.WriteLine("1 - gore desno   2 - gore lijevo");
                        Console.WriteLine("3 - dole desno   4 - dole lijevo");
                        int orijent = int.Parse(Console.ReadLine());

                        Podmornica nova = new Podmornica(start, orijent, dimenzija);

                        bool sudara = false;
                        foreach (var postojeca in svePodmornice)
                        {
                            if (Podmornica.Dodiruje(postojeca, nova.Polja, dimenzija))
                            {
                                sudara = true;
                            }
                        }

                        if (sudara)
                        {
                            Console.WriteLine("[GRESKA] Nova podmornica se poklapa ili dodiruje sa vec postojecim!");
                            continue;
                        }

                        svePodmornice.Add(nova.Polja);
                        Console.WriteLine($"Podmornica {i + 1} postavljena: {string.Join(",", nova.Polja)}");
                        break;

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[GRESKA] " + ex.Message);
                    }
                }
            }

            // slanje podmornica na server
            string porukaZaServer = string.Join(";", svePodmornice.ConvertAll(p => string.Join(",", p)));
            byte[] data = Encoding.UTF8.GetBytes(porukaZaServer);
            clientSocket.Send(data);

            Console.WriteLine($"[TCP] Poslate podmornice serveru: {porukaZaServer}");

            StanjeKlijenta stanje = StanjeKlijenta.BiranjeMeta;

            while (true)
            {
                byte[] buffer = new byte[2048];
                int napdaBr;

                try
                {
                    napdaBr = clientSocket.Receive(buffer);
                }
                catch
                {
                    Console.WriteLine("Server je zatvorio vezu.");
                    break;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, napdaBr);

                if (msg.Contains("Cestitamo") || msg.Contains("Nazalost"))
                {
                    Console.WriteLine(msg);
                    break;
                }

                if (msg.StartsWith("Izaberite"))
                {
                    if (stanje == StanjeKlijenta.BiranjeMeta)
                    {
                        Console.WriteLine(msg);
                        Console.Write("Unesite ID protivnika: ");
                        string targetId = Console.ReadLine();
                        clientSocket.Send(Encoding.UTF8.GetBytes(targetId));

                        stanje = StanjeKlijenta.UnosPolja;
                    }
                    continue;
                }
                else if (msg.Contains("0") || msg.Contains("#") || msg.Contains("X"))
                {

                    if (stanje == StanjeKlijenta.UnosPolja)
                    {
                        Console.WriteLine("Tabela protivnika:");
                        Console.WriteLine(msg);

                        Console.Write("Unesite broj polja koje gadjate: ");
                        string polje = Console.ReadLine();
                        clientSocket.Send(Encoding.UTF8.GetBytes(polje));

                        stanje = StanjeKlijenta.Idle;
                    }
                    continue;
                }
                else if (msg.StartsWith("[UPDATE]"))
                {
                    Console.WriteLine(msg);
                    continue;
                }
                else
                {
                    Console.WriteLine(msg);

                    if (msg.Contains("PROMASIO") || msg.Contains("POGODIO") || msg.Contains("POTOPIO"))
                    {
                        stanje = StanjeKlijenta.BiranjeMeta;
                    }
                }
            }
        }
    }
}
