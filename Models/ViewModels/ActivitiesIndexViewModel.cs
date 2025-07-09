using X.PagedList;

namespace WebApplication5.Models.ViewModels
{
    public class ActivitiesIndexViewModel
    {
        public IPagedList<Activity> AssignedActivities { get; set; }
        public IPagedList<Activity> UnassignedActivities { get; set; }
    }
}
