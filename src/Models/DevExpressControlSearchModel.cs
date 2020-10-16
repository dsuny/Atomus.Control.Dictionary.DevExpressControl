namespace Atomus.Control.Dictionary.Models
{
    internal class DevExpressControlSearchModel : Mvc.Models
    {
        internal string CODE { get; set; }
        internal string SEARCH_TEXT { get; set; }
        internal int SEARCH_INDEX { get; set; }
        internal string COND_ETC { get; set; }
        internal string SEARCH_ALL { get; set; }
        internal string STARTS_WITH { get; set; }
    }
}