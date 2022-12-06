using System.Threading;
using System.Threading.Tasks;
using Storex;

namespace AuthenticationModules
{
    [AuthenticationModuleExport("faf8d1ba-dd95-48d8-8f2e-146c3ad81681", "簡易認証 (氏名選択) [C#]", Description = "CSVファイル (ID と 氏名) を使用します")]
    public class UseCsvFileAuthenticationModule : IAuthenticationModule
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
            IUser user = AuthenticationForm2.Authenticate();

            return Task.FromResult(user);
        }

        public void Dispose()
        {
        }
    }
}