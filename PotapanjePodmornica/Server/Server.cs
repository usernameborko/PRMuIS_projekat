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

            // cuvanje podmornica
            List<Igrac> igraci = new List<Igrac>();
            int idBrojac = 1;

            foreach (var klijent in tcpKlijenti)
            {
                byte[] podmorniceBuffer = new byte[1024];
                int brBajta = klijent.Receive(podmorniceBuffer);
                string podatak = Encoding.UTF8.GetString(podmorniceBuffer, 0, brBajta);
                Console.WriteLine($"[TCP] Podmornice of {klijent.RemoteEndPoint}: {podatak}");

                List<List<int>> listaPodmornica = new List<List<int>>();
                string[] sve = podatak.Split(';');
                foreach (var subs in sve)
                {
                    List<int> polja = new List<int>();
                    foreach (var broj in subs.Split(","))
                    {
                        if (int.TryParse(broj, out int p))
                        {
                            polja.Add(p);
                        }
                    }
                    listaPodmornica.Add(polja);
                }

                Igrac igrac = new Igrac(idBrojac++, klijent, dimenzija, listaPodmornica);

                igraci.Add(igrac);

                Console.WriteLine($"[INFO] Igrac {klijent.RemoteEndPoint} je poslao podmornice: ");
                for (int i = 0; i < listaPodmornica.Count; i++)
                {
                    Console.WriteLine($"    Podmornica {i + 1}: {string.Join(",", listaPodmornica[i])}");
                }
            }

            Console.WriteLine("[INFO] Svi igraci su poslali svoje podmornice!");

            // jedan potez zadatak 5
            int trenutniIgracIndex = 0;
            bool igraGotova = false;

            while (!igraGotova)
            {
                var aktivni = igraci.Where(i => i.Podmornice.Any(p => p.Count > 0) && i.Promasaji < promasaji).ToList();

                if (aktivni.Count == 0)
                {
                    igraGotova = true;
                    break;
                }

                if (trenutniIgracIndex >= aktivni.Count)
                    trenutniIgracIndex = 0;

                Igrac napadac = aktivni[trenutniIgracIndex];
                int pozicijaUStaroAktivni = trenutniIgracIndex;

                StringBuilder listaMeta = new StringBuilder("Izaberite kojeg igraca gadjate:\n");
                foreach (var meta in aktivni)
                {
                    if (meta.Id != napadac.Id)
                        listaMeta.AppendLine($"Igrac {meta.Id}");
                }
                napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes(listaMeta.ToString()));

                byte[] napadBuffer = new byte[1024];

                List<Socket> checkRead = new List<Socket> { napadac.KlijentSocket };
                Socket.Select(checkRead, null, null, 10000000);

                Igrac metaIgrac = null;

                if (checkRead.Contains(napadac.KlijentSocket))
                {
                    int napadacBr = napadac.KlijentSocket.Receive(napadBuffer);
                    string data = Encoding.UTF8.GetString(napadBuffer, 0, napadacBr).Trim();

                    if (!int.TryParse(data, out int izabraniId))
                    {
                        napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes("[GRESKA] Nevalidan ID."));
                        continue;
                    }

                    metaIgrac = aktivni.Find(x => x.Id == izabraniId);

                    if (metaIgrac == null || metaIgrac.Id == napadac.Id)
                    {
                        napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes("[GRESKA] Nevalidan izbor protivnika."));
                        continue;
                    }

                    string prikaz = PrikaziTabelu(metaIgrac, dimenzija);
                    napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes(prikaz));
                }
                else
                {
                    continue;
                }

                List<Socket> checkReadPolje = new List<Socket> { napadac.KlijentSocket };
                Socket.Select(checkReadPolje, null, null, 90000000);

                int polje = -1;
                if (checkReadPolje.Contains(napadac.KlijentSocket))
                {
                    int brPolje = napadac.KlijentSocket.Receive(napadBuffer);
                    string dataPolje = Encoding.UTF8.GetString(napadBuffer, 0, brPolje).Trim();

                    if (!int.TryParse(dataPolje, out polje))
                    {
                        napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes("[GRESKA] Unesite validan broj polja!"));
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                int row = (polje - 1) / dimenzija;
                int col = (polje - 1) % dimenzija;
                string ishod = "PROMASIO";

                if (metaIgrac.Tabla[row, col] != 0)
                {
                    napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes("[GRESKA] Ovo polje je vec gadjano! izaberite novo polje."));
                    continue;
                }

                foreach (var pod in metaIgrac.Podmornice)
                {
                    if (pod.Contains(polje))
                    {
                        pod.Remove(polje);
                        metaIgrac.Tabla[row, col] = 2;
                        if (pod.Count == 0)
                        {
                            ishod = "POTOPIO";
                        }
                        else
                        {
                            ishod = "POGODIO";
                        }
                        break;
                    }
                }

                if (ishod == "PROMASIO")
                {
                    metaIgrac.Tabla[row, col] = 1;
                    napadac.Promasaji++;
                }
                else
                {
                    napadac.Pogoci++;
                }

                napadac.KlijentSocket.Send(Encoding.UTF8.GetBytes(ishod));

                Console.WriteLine($"[LOG] Igrac {napadac.Id} -> Igrac {metaIgrac.Id}: polje {polje}, {ishod}");

                PosaljiAzuriranuTabeluSvima(igraci, metaIgrac, dimenzija);

                if (napadac.Promasaji >= promasaji)
                {
                    Console.WriteLine($"[INFO] Igrac {napadac.Id} eliminisan zbog previše promašaja!");
                    napadac.Podmornice.Clear();
                }

                var noviAktivni = igraci.Where(i => i.Podmornice.Any(p => p.Count > 0) && i.Promasaji < promasaji).ToList();

                if (noviAktivni.Count <= 1)
                {
                    igraGotova = true;

                    Console.WriteLine("[KRAJ IGRE]");

                    foreach (var ig in igraci)
                    {
                        if (noviAktivni.Contains(ig))
                        {
                            ig.KlijentSocket.Send(Encoding.UTF8.GetBytes($"Cestitamo, pobedili ste! Broj pogodaka: {ig.Pogoci}"));
                        }
                        else
                        {
                            ig.KlijentSocket.Send(Encoding.UTF8.GetBytes($"Nazalost, izgubili ste. Pogodaka: {ig.Pogoci}"));
                        }
                    }

                    foreach (var ig in igraci)
                    {
                        try
                        {
                            ig.KlijentSocket.Close();
                        }
                        catch { }
                    }
                    tcpSocket.Close();

                    Console.WriteLine("\n=== Rang lista ===");
                    foreach (var ig in igraci.OrderByDescending(x => x.Podmornice.Sum(p => p.Count)).ThenByDescending(x => x.Pogoci))
                    {
                        int preostale = ig.Podmornice.Sum(p => p.Count);
                        Console.WriteLine($"Igrac {ig.Id}: preostale celije {preostale}, pogodaka {ig.Pogoci}");
                    }
                    break;
                }

                if (napadac.Promasaji >= promasaji)
                {
                    trenutniIgracIndex = pozicijaUStaroAktivni % noviAktivni.Count;
                }
                else
                {
                    int newIndex = noviAktivni.FindIndex(x => x.Id == napadac.Id);
                    if (ishod == "PROMASIO")
                    {
                        trenutniIgracIndex = (newIndex + 1) % noviAktivni.Count;
                    }
                    else
                    {
                        trenutniIgracIndex = newIndex;
                    }
                }
            }

        }

        static string PrikaziTabelu(Igrac meta, int dim)
        {
            StringBuilder sb = new StringBuilder();
            for (int r = 0; r < dim; r++)
            {
                for (int c = 0; c < dim; c++)
                {
                    int stanje = meta.Tabla[r, c];
                    if (stanje == 0)
                    {
                        sb.Append("0 ");
                    }
                    else if (stanje == 1)
                    {
                        sb.Append("# ");
                    }
                    else if (stanje == 2)
                    {
                        sb.Append("X ");
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static void PosaljiAzuriranuTabeluSvima(List<Igrac> igraci, Igrac metaIgrac, int dimenzija)
        {
            string prikazTable = PrikaziTabelu(metaIgrac, dimenzija);
            string poruka = $"[UPDATE] Stanje table Igraca {metaIgrac.Id} nakon poteza:\n{prikazTable}\n";
            byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

            foreach (var igrac in igraci)
            {
                try
                {
                    igrac.KlijentSocket.Send(porukaBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GRESKA] Slanje table igracu {igrac.Id}: {ex.Message}");
                }
            }
        }
    }
}
