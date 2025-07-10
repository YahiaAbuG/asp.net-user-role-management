using X.PagedList;

namespace WebApplication5.Models.ViewModels
{
    public class ActivitiesIndexViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<string> Admins { get; set; } = new();
        public List<string> Members { get; set; } = new();
    }

}
