using Microsoft.Azure.Search;
using System;
using System.Collections.Generic;
using System.Text;

namespace FuncSearch
{
    public partial class MfrCode
    {

        [IsFilterable, IsSortable, IsFacetable]
        public string rid { get; set; }
        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string avnetmfscode { get; set; }
        [IsFilterable, IsSortable, IsFacetable]
        public string custname { get; set; }
       
        public string score { get; set; }
        public string exclusionFlag { get; set; }
        public string avnetmfscodeNotFoundFlag { get; set; }
    }

    public class PostData
    {
        public string[] name { get; set; }
    }
    public class ReturnData
    {
        public List<MfrCode> results;
    }
}
