using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsHelperLib
{
    public class TfsChangeset
    {
        public int ChangesetId { get; set; }

        public string Owner { get; set; }

        public DateTime CreationDate { get; set; }

        public string Comment { get; set; }

        public string Changes_0_ServerItem { get; set; }
    }
}
