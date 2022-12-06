Imports System.Threading
Imports Storex

<AuthenticationModuleExport("A39FFBE1-2963-4A02-AA0F-75F8E2595DDC", "簡易認証 (ID入力) [VB]", Description:="CSVファイル (ID と 氏名) を使用します")>
Public Class AuthenticationModule1
    Implements IAuthenticationModule

    Public ReadOnly Property IsConfiguable As Boolean Implements IAuthenticationModule.IsConfiguable
        Get
            IsConfiguable = True
        End Get
    End Property

    Public Function ConfigureAsync() As Task Implements IAuthenticationModule.ConfigureAsync

        AuthenticationModuleHelper.PickUsersFileAndSaveToSetting()

        Return Task.CompletedTask
    End Function

    Public Async Function AuthenticateAsync() As Task(Of IUser) Implements IAuthenticationModule.AuthenticateAsync

        Return Await AuthenticateAsync(cancellationToken:=CancellationToken.None)
    End Function

    Public Function AuthenticateAsync(cancellationToken As CancellationToken) As Task(Of IUser) Implements IAuthenticationModule.AuthenticateAsync

        Dim User As IUser = AuthenticationForm1.Authenticate

        Return Task.FromResult(User)
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
