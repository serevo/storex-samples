using System.Threading;
using System.Threading.Tasks;
using Storex;

namespace AuthenticationModules
{
    [AuthenticationModuleExport("FCB29577-7E5B-4B0C-A514-B8E636AAF13D", "簡易認証 (ID入力) [C#]", Description = "CSVファイル (ID と 氏名) を使用します")]
    public class AuthenticationModule1 : IAuthenticationModule
    {
        public bool IsConfiguable => true;

        public Task ConfigureAsync()
        {
            AuthenticationModuleHelper.PickUsersFileAndSaveToSetting();

            return Task.CompletedTask;
        }

        public async Task<IUser> AuthenticateAsync()
        {
            return await AuthenticateAsync(cancellationToken: CancellationToken.None);
        }

        public Task<IUser> AuthenticateAsync(CancellationToken cancellationToken)
        {
            IUser user = AuthenticationForm1.Authenticate();

            return Task.FromResult(user);
        }

        public void Dispose()
        {
        }
    }
}