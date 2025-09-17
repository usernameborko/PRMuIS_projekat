using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Helpers
{
    public class Igrac
    {
        public int Id { get; set; }
        public Socket KlijentSocket { get; set; }
        public int Promasaji { get; set; } = 0;
        public int Pogoci { get; set; } = 0;
        public List<List<int>> Podmornice { get; set; } = new List<List<int>>();
        public int[,] Tabla { get; set; }

        public Igrac(int id, Socket soket, int dimenzija, List<List<int>> podmornice)
        {
            Id = id;
            KlijentSocket = soket;
            Podmornice = podmornice;
            Tabla = new int[dimenzija, dimenzija];
        }
    }
}
