using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubStatApi.models
{
    public class RepoData
    {
        public string? name { get; set; }
        public string? html_url { get; set; }
        public string? allLanguages { get; set; }
        public string? description { get; set; }
        public bool _private { get; set; }
    }
}
