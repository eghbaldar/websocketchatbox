using websocket.Context;

namespace websocket.Services.Users.Authentication
{
    public class CheckAuthentication
    {
        private readonly IDataBaseContext _context;

        public CheckAuthentication(IDataBaseContext context)
        {
            _context = context;
        }

        public Guid Check(string _username, string _password)
        {
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password)) { return Guid.Empty; }
            var check = _context.Users.FirstOrDefault(x => x.Username == _username && x.Password == _password);
            if(check == null) { return Guid.Empty; }
            return check.Id;
        }
    }

}
