using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicoServidorSIS
{
    public class Remota
    {
        public int IdRemota { get; set; }
        public string IMEI { get; set; }
        public string IP { get; set; }
        public string Email { get; set; }
        public DateTime DataHoraUltimoAlteração { get; set; }
        public int FlagConf { get; set; } // flag indicando se envia a remota a configuração itallux

        public string Longitude { get; set; }
        public string Latitude { get; set; }

    }

}
