namespace NewsSite1.Models
{
    public class SiteStatistics
    {
        private int totalUsers;
        private int totalArticles;
        private int totalSaved;
        private int todayLogins;
        private int todayFetches;
        private int totalLikes;
        private int todaysLikes;

        public SiteStatistics() { }

        public SiteStatistics(int totalUsers, int totalArticles, int totalSaved,
                              int todayLogins, int todayFetches,
                              int totalLikes, int todaysLikes)
        {
            this.totalUsers = totalUsers;
            this.totalArticles = totalArticles;
            this.totalSaved = totalSaved;
            this.todayLogins = todayLogins;
            this.todayFetches = todayFetches;
            this.totalLikes = totalLikes;
            this.todaysLikes = todaysLikes;
        }

        public int TotalUsers { get => totalUsers; set => totalUsers = value; }
        public int TotalArticles { get => totalArticles; set => totalArticles = value; }
        public int TotalSaved { get => totalSaved; set => totalSaved = value; }
        public int TodayLogins { get => todayLogins; set => todayLogins = value; }
        public int TodayFetches { get => todayFetches; set => todayFetches = value; }
        public int TotalLikes { get => totalLikes; set => totalLikes = value; }
        public int TodaysLikes { get => todaysLikes; set => todaysLikes = value; }
    }
}
