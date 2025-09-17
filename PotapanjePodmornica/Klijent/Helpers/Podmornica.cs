using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klijent.Helpers
{
    public class Podmornica
    {
        public List<int> Polja { get; private set; }

        public Podmornica(int start, int orijentacija, int dimenzija)
        {
            Polja = Generisi(start, orijentacija, dimenzija);
        }

        private List<int> Generisi(int start, int orijentacija, int dimenzija)
        {
            List<int> polja = new List<int>();

            int idx = start - 1;
            int r = idx / dimenzija;
            int c = idx % dimenzija;

            polja.Add(start);

            switch (orijentacija)
            {
                case 1:
                    if (r - 1 >= 0 && c + 1 < dimenzija)
                    {
                        polja.Add(r * dimenzija + (c + 1) + 1);
                        polja.Add((r - 1) * dimenzija + c + 1);
                    }
                    break;
                case 2:
                    if (r - 1 >= 0 && c - 1 >= 0)
                    {
                        polja.Add(r * dimenzija + (c - 1) + 1);
                        polja.Add((r - 1) * dimenzija + c + 1);
                    }
                    break;
                case 3:
                    if (r + 1 < dimenzija && c + 1 < dimenzija)
                    {
                        polja.Add(r * dimenzija + (c + 1) + 1);
                        polja.Add((r + 1) * dimenzija + c + 1);
                    }
                    break;
                case 4:
                    if (r + 1 < dimenzija && c - 1 >= 0)
                    {
                        polja.Add(r * dimenzija + (c - 1) + 1);
                        polja.Add((r + 1) * dimenzija + c + 1);
                    }
                    break;
            }

            if (polja.Count != 3)
            {
                throw new Exception("Podmornica ne moze da stane na datu poziciju!");
            }

            return polja;
        }

        public static bool Dodiruje(List<int> set1, List<int> set2, int dimenzija)
        {
            foreach (var p1 in set1)
            {
                int r1 = (p1 - 1) / dimenzija;
                int c1 = (p1 - 1) % dimenzija;

                foreach (var p2 in set2)
                {
                    int r2 = (p2 - 1) / dimenzija;
                    int c2 = (p2 - 1) % dimenzija;

                    if (Math.Abs(r1 - r2) <= 1 && Math.Abs(c1 - c2) <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
