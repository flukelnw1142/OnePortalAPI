

namespace OnePortal_Api.Dto
{
    public class UserDto
    {

        //public int user_id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public int Role { get; set; }
        public int status { get; set; }
        public DateTime create_date { get; set; }
        public DateTime update_date { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string company { get; set; }
    }
}
